// SPDX-License-Identifier: GPL-2.0-or-later
// Desktop State Tracker - GNOME Shell Extension
// Provides D-Bus API for desktop context awareness

// Type declarations are in gjs.d.ts
/// <reference path="./gjs.d.ts" />

import Gio from 'gi://Gio';
import GLib from 'gi://GLib';
import Shell from 'gi://Shell';
import St from 'gi://St';
import Atspi from 'gi://Atspi';
import { Extension } from 'resource:///org/gnome/shell/extensions/extension.js';
import * as Main from 'resource:///org/gnome/shell/ui/main.js';
import * as FocusCaretTracker from 'resource:///org/gnome/shell/ui/focusCaretTracker.js';

// Internal types
interface GObjectWithSignals {
    connect(signal: string, callback: (...args: unknown[]) => void): number;
    disconnect(id: number): void;
}

interface SignalConnection {
    object: GObjectWithSignals;
    id: number;
}

interface DBusExportedObject {
    export(connection: unknown, path: string): void;
    unexport(): void;
    emit_signal(name: string, variant: unknown): void;
    emit_property_changed(name: string, variant: unknown): void;
}

// Cursor rectangle from AT-SPI
interface CursorRect {
    x: number;
    y: number;
    width: number;
    height: number;
}

// D-Bus interface definition
const INTERFACE_XML = `
<node>
  <interface name="org.olbrasoft.Desktop">
    <!-- Properties -->
    <property name="CurrentWorkspace" type="i" access="read"/>
    <property name="TotalWorkspaces" type="i" access="read"/>
    <property name="ActiveWindow" type="s" access="read"/>
    <property name="ActiveApplication" type="s" access="read"/>

    <!-- Methods -->
    <method name="GetWorkspaceApplications">
      <arg type="i" direction="in" name="workspaceIndex"/>
      <arg type="a(sss)" direction="out" name="applications"/>
    </method>

    <method name="GetPointerPosition">
      <arg type="s" direction="out" name="positionJson"/>
    </method>

    <method name="GetCaretPosition">
      <arg type="s" direction="out" name="caretJson"/>
    </method>

    <method name="GetActiveWindowGeometry">
      <arg type="s" direction="out" name="geometryJson"/>
    </method>

    <method name="ShowRecordingOverlay">
      <arg type="s" direction="in" name="text"/>
    </method>

    <method name="HideRecordingOverlay"/>

    <!-- Signals -->
    <signal name="WorkspaceChanged">
      <arg type="i" name="newIndex"/>
      <arg type="i" name="totalWorkspaces"/>
    </signal>

    <signal name="FocusChanged">
      <arg type="s" name="windowTitle"/>
      <arg type="s" name="appId"/>
      <arg type="s" name="wmClass"/>
    </signal>
  </interface>
</node>`;

/**
 * Desktop state service providing D-Bus methods and signals
 * for workspace, window, and cursor tracking.
 *
 * Uses AT-SPI FocusCaretTracker for universal caret position tracking.
 * This approach works for all accessibility-compliant applications:
 * GTK, Qt, Electron/Chrome, Firefox, terminals (VTE-based), etc.
 *
 * Based on: GNOME Shell magnifier.js implementation
 *
 * NOTE: GPU-rendered terminals (kitty, Alacritty) don't support AT-SPI
 * and will not provide caret position. This is a terminal limitation.
 */
class DesktopStateService {
    private _workspaceManager: WorkspaceManager;
    private _display: Display;
    private _tracker: ShellWindowTracker;
    private _signalIds: SignalConnection[];
    private _overlayWidget: St.BoxLayout | null = null;
    private _overlayLabel: St.Label | null = null;
    public _impl: DBusExportedObject | null = null;

    // Caret position from AT-SPI (universal approach)
    // Based on: magnifier.js _updateCaret implementation
    private _cursorRect: CursorRect | null = null;
    private _focusCaretTracker: FocusCaretTrackerInstance | null = null;
    private _caretMovedSignalId: number | null = null;

    constructor() {
        this._workspaceManager = global.workspace_manager;
        this._display = global.display;
        this._tracker = Shell.WindowTracker.get_default() as ShellWindowTracker;
        this._signalIds = [];

        // Initialize AT-SPI FocusCaretTracker for universal caret tracking
        // This is the same approach used by GNOME Shell magnifier.js
        try {
            this._focusCaretTracker = new FocusCaretTracker.FocusCaretTracker() as FocusCaretTrackerInstance;

            // Listen for caret-moved events (object:text-caret-moved AT-SPI signal)
            this._caretMovedSignalId = this._focusCaretTracker.connect(
                'caret-moved',
                this._onCaretMoved.bind(this)
            );

            // Register the caret listener to start receiving AT-SPI events
            this._focusCaretTracker.registerCaretListener();

            log('[DesktopState] FocusCaretTracker initialized and listening for caret-moved events');
        } catch (e) {
            logError(e as Error, '[DesktopState] Failed to initialize FocusCaretTracker');
        }

        log('[DesktopState] Service initialized');

        // Connect to workspace change signals
        const workspaceSwitchedId = this._workspaceManager.connect(
            'workspace-switched',
            this._onWorkspaceChanged.bind(this)
        );
        this._signalIds.push({ object: this._workspaceManager, id: workspaceSwitchedId });

        const nWorkspacesSignalId = this._workspaceManager.connect(
            'notify::n-workspaces',
            this._onWorkspacesCountChanged.bind(this)
        );
        this._signalIds.push({ object: this._workspaceManager, id: nWorkspacesSignalId });

        // Connect to focus change signals
        const focusWindowId = this._display.connect(
            'notify::focus-window',
            this._onFocusChanged.bind(this)
        );
        this._signalIds.push({ object: this._display, id: focusWindowId });

        const focusAppId = this._tracker.connect(
            'notify::focus-app',
            this._onFocusChanged.bind(this)
        );
        this._signalIds.push({ object: this._tracker, id: focusAppId });
    }

    destroy(): void {
        this.destroyOverlay();

        // Disconnect and deregister FocusCaretTracker
        if (this._focusCaretTracker) {
            try {
                if (this._caretMovedSignalId !== null) {
                    this._focusCaretTracker.disconnect(this._caretMovedSignalId);
                    this._caretMovedSignalId = null;
                }
                this._focusCaretTracker.deregisterCaretListener();
            } catch (e) {
                // Ignore cleanup errors
            }
            this._focusCaretTracker = null;
        }

        this._signalIds.forEach((signal) => {
            signal.object.disconnect(signal.id);
        });
        this._signalIds = [];

        log('[DesktopState] Service destroyed');
    }

    /**
     * Called when caret moves in any AT-SPI compliant text widget.
     * Based on magnifier.js _updateCaret implementation.
     *
     * KEY INSIGHT from Orca/magnifier.js:
     * - Use WINDOW coordinates (not SCREEN) from AT-SPI
     * - Convert to screen space using focusWindow.get_client_content_rect()
     * - Apply scale factor for HiDPI displays
     *
     * This approach works for ALL applications including terminals on Wayland,
     * because AT-SPI provides window-relative coordinates that we then convert
     * using Mutter's window geometry (which IS accurate on Wayland).
     *
     * @param _tracker - FocusCaretTracker instance
     * @param event - AT-SPI caret event with source accessible
     */
    private _onCaretMoved(_tracker: unknown, event: AtspiCaretEvent): void {
        try {
            if (!event.source) {
                return;
            }

            // Get the text interface from the accessible source
            const text = event.source.get_text_iface();
            if (!text) {
                return;
            }

            const caretOffset = text.get_caret_offset();

            // Get character extents in WINDOW coordinates (like magnifier.js does)
            // This is more reliable than SCREEN coordinates, especially on Wayland
            const windowExtents = text.get_character_extents(caretOffset, Atspi.CoordType.WINDOW);

            // Ignore if extents are 0x0 (no valid position)
            if (windowExtents.width === 0 && windowExtents.height === 0) {
                log(`[DesktopState] Caret extents are 0x0, ignoring`);
                return;
            }

            // Convert WINDOW coordinates to SCREEN coordinates
            // This is the key insight from magnifier.js - use Mutter's window geometry
            const screenExtents = this._convertExtentsToScreenSpace(event.source, windowExtents);
            if (!screenExtents) {
                return;
            }

            this._cursorRect = {
                x: screenExtents.x,
                y: screenExtents.y,
                width: Math.max(screenExtents.width, 1),
                height: Math.max(screenExtents.height, 1)
            };

            log(`[DesktopState] Caret moved: (${this._cursorRect.x}, ${this._cursorRect.y}) ${this._cursorRect.width}x${this._cursorRect.height}`);
        } catch (e) {
            // Log but don't crash - AT-SPI can be unreliable
            log(`[DesktopState] Failed to read caret extents: ${(e as Error).message}`);
        }
    }

    /**
     * Convert AT-SPI WINDOW coordinates to screen space.
     * Based on magnifier.js _convertExtentsToScreenSpace.
     *
     * This is necessary because:
     * 1. AT-SPI SCREEN coordinates are unreliable on Wayland (often 0,0)
     * 2. AT-SPI WINDOW coordinates are reliable but need conversion
     * 3. Mutter knows the actual window position on screen
     *
     * @param accessible - The AT-SPI accessible object
     * @param extents - Extents in WINDOW coordinates
     * @returns Extents in SCREEN coordinates, or null if conversion failed
     */
    private _convertExtentsToScreenSpace(
        accessible: AtspiAccessible,
        extents: { x: number; y: number; width: number; height: number }
    ): { x: number; y: number; width: number; height: number } | null {
        // Validate the accessible is from a focused window
        // (Skip validation for gnome-shell's own events)
        try {
            let app: AtspiAccessible | null = null;
            let parentWindow: AtspiAccessible | null = null;
            let iter: AtspiAccessible | null = accessible;

            const toplevelWindowTypes = new Set([
                Atspi.Role.FRAME,
                Atspi.Role.DIALOG,
                Atspi.Role.WINDOW,
            ]);

            while (iter) {
                const role = iter.get_role();
                if (role === Atspi.Role.APPLICATION) {
                    app = iter;
                    break;
                } else if (toplevelWindowTypes.has(role)) {
                    parentWindow = iter;
                }
                iter = iter.get_parent();
            }

            // Skip our own events (already in screen coordinates)
            if (app && app.get_name() === 'gnome-shell') {
                return extents;
            }

            // Verify the window is active and accessible is focused
            if (parentWindow) {
                const stateSet = parentWindow.get_state_set();
                const accessibleStateSet = accessible.get_state_set();
                if (stateSet && accessibleStateSet) {
                    const windowActive = stateSet.contains(Atspi.StateType.ACTIVE);
                    const accessibleFocused = accessibleStateSet.contains(Atspi.StateType.FOCUSED);
                    if (!windowActive || !accessibleFocused) {
                        return null;
                    }
                }
            }
        } catch (e) {
            log(`[DesktopState] Failed to validate parent window: ${(e as Error).message}`);
        }

        // Get the focused window from Mutter
        const focusWindow = this._display.focus_window;
        if (!focusWindow) {
            return null;
        }

        // Get window content rectangle (excludes decorations)
        const windowRect = focusWindow.get_client_content_rect
            ? focusWindow.get_client_content_rect()
            : focusWindow.get_frame_rect();

        // Get scale factor for HiDPI displays
        const scaleFactor = St.ThemeContext.get_for_stage(global.stage).scale_factor;

        // Convert to screen coordinates
        const screenExtents = {
            x: windowRect.x + (scaleFactor * extents.x),
            y: windowRect.y + (scaleFactor * extents.y),
            width: scaleFactor * extents.width,
            height: scaleFactor * extents.height,
        };

        return screenExtents;
    }

    // Clear cursor position when focus changes (user might switch to non-text app)
    private _onFocusChanged(): void {
        const window = this._display.focus_window;
        const app = this._tracker.focus_app;

        const windowTitle = window ? window.get_title() ?? '' : '';
        const appId = app ? app.get_id() : '';
        const wmClass = window ? window.get_wm_class() ?? '' : '';

        log(`[DesktopState] Focus changed: ${windowTitle} (${appId}) [${wmClass}]`);

        // Clear cursor position on focus change - will be updated by caret-moved event
        this._cursorRect = null;

        if (this._impl) {
            this._impl.emit_signal('FocusChanged', GLib.Variant.new('(sss)', [windowTitle, appId, wmClass]));
            this._impl.emit_property_changed('ActiveWindow', GLib.Variant.new_string(windowTitle));
            this._impl.emit_property_changed('ActiveApplication', GLib.Variant.new_string(appId));
        }
    }

    // Property getters (must return GLib.Variant for D-Bus)
    get CurrentWorkspace(): unknown {
        const index = this._workspaceManager.get_active_workspace_index();
        log(`[DesktopState] Get CurrentWorkspace: ${index}`);
        return GLib.Variant.new_int32(index);
    }

    get TotalWorkspaces(): unknown {
        const total = this._workspaceManager.n_workspaces;
        log(`[DesktopState] Get TotalWorkspaces: ${total}`);
        return GLib.Variant.new_int32(total);
    }

    get ActiveWindow(): unknown {
        const window = this._display.focus_window;
        const title = window ? window.get_title() ?? '' : '';
        log(`[DesktopState] Get ActiveWindow: ${title}`);
        return GLib.Variant.new_string(title);
    }

    get ActiveApplication(): unknown {
        const app = this._tracker.focus_app;
        const appId = app ? app.get_id() : '';
        log(`[DesktopState] Get ActiveApplication: ${appId}`);
        return GLib.Variant.new_string(appId);
    }

    // D-Bus Methods
    GetWorkspaceApplications(workspaceIndex: number): unknown {
        log(`[DesktopState] GetWorkspaceApplications called for workspace: ${workspaceIndex}`);

        const totalWorkspaces = this._workspaceManager.n_workspaces;

        // Validate workspace index
        if (workspaceIndex < 0 || workspaceIndex >= totalWorkspaces) {
            log(`[DesktopState] Invalid workspace index: ${workspaceIndex} (total: ${totalWorkspaces})`);
            return GLib.Variant.new('a(sss)', []);
        }

        const workspace = this._workspaceManager.get_workspace_by_index(workspaceIndex);
        if (!workspace) {
            log(`[DesktopState] Workspace not found: ${workspaceIndex}`);
            return GLib.Variant.new('a(sss)', []);
        }

        // Get all windows on this workspace
        const windows = workspace.list_windows();
        const applications: [string, string, string][] = [];

        for (const window of windows) {
            // Skip windows that shouldn't be tracked (skip_taskbar)
            if (window.skip_taskbar) {
                continue;
            }

            const windowTitle = window.get_title() ?? '';
            const wmClass = window.get_wm_class() ?? '';

            // Get application for this window
            const app = this._tracker.get_window_app(window);
            const appId = app ? app.get_id() : '';

            applications.push([appId, windowTitle, wmClass]);

            log(`[DesktopState] Found window on workspace ${workspaceIndex}: ${windowTitle} (${appId}) [${wmClass}]`);
        }

        log(`[DesktopState] Returning ${applications.length} applications for workspace ${workspaceIndex}`);
        return applications;
    }

    GetPointerPosition(): string {
        const [x, y] = global.get_pointer();
        log(`[DesktopState] GetPointerPosition: (${x}, ${y})`);
        return JSON.stringify({ x: Math.floor(x), y: Math.floor(y) });
    }

    GetCaretPosition(): string {
        // Return cursor position from AT-SPI FocusCaretTracker if available
        if (this._cursorRect) {
            log(`[DesktopState] GetCaretPosition: (${this._cursorRect.x}, ${this._cursorRect.y}) from AT-SPI`);
            return JSON.stringify({
                available: true,
                x: this._cursorRect.x,
                y: this._cursorRect.y,
                width: this._cursorRect.width,
                height: this._cursorRect.height,
                source: 'atspi'
            });
        }

        log('[DesktopState] GetCaretPosition: no cursor position available');
        return JSON.stringify({ available: false, reason: 'no_cursor_location' });
    }

    GetActiveWindowGeometry(): string {
        const window = this._display.focus_window;
        if (!window) {
            log('[DesktopState] GetActiveWindowGeometry: no focused window');
            return JSON.stringify({ x: 0, y: 0, width: 0, height: 0 });
        }

        const rect = window.get_frame_rect();
        log(`[DesktopState] GetActiveWindowGeometry: (${rect.x}, ${rect.y}, ${rect.width}, ${rect.height})`);
        return JSON.stringify({ x: rect.x, y: rect.y, width: rect.width, height: rect.height });
    }

    ShowRecordingOverlay(text: string): void {
        // Try to get caret position first, fall back to mouse position
        const position = this._getOverlayPosition();
        log(`[DesktopState] ShowRecordingOverlay: "${text}" at (${position.x}, ${position.y}) source=${position.source}`);

        if (!this._overlayWidget) {
            this._createOverlayWidget();
        }

        if (this._overlayLabel) {
            this._overlayLabel.text = text || 'Recording...';
        }

        if (this._overlayWidget) {
            const overlayWidth = 180;
            const overlayHeight = 40;
            const verticalOffset = 20;

            // Position overlay above the caret/cursor
            this._overlayWidget.x = Math.floor(position.x) - overlayWidth / 2;
            this._overlayWidget.y = Math.floor(position.y) - overlayHeight - verticalOffset;
            this._overlayWidget.visible = true;
        }
    }

    private _getOverlayPosition(): { x: number; y: number; source: string } {
        // Try cursor position from AT-SPI first
        if (this._cursorRect) {
            return {
                x: this._cursorRect.x + this._cursorRect.width / 2,
                y: this._cursorRect.y + this._cursorRect.height,
                source: 'caret'
            };
        }

        // Fall back to mouse position
        const [mouseX, mouseY] = global.get_pointer();
        return {
            x: mouseX,
            y: mouseY,
            source: 'mouse'
        };
    }

    HideRecordingOverlay(): void {
        log('[DesktopState] HideRecordingOverlay');

        if (this._overlayWidget) {
            this._overlayWidget.visible = false;
        }
    }

    private _createOverlayWidget(): void {
        this._overlayWidget = new St.BoxLayout({
            style_class: 'recording-overlay',
            vertical: false,
        });

        this._overlayWidget.set_style(
            'background-color: rgba(40, 40, 40, 0.95); ' +
            'border-radius: 8px; ' +
            'border: 1px solid rgba(255, 120, 0, 0.5); ' +
            'padding: 8px 12px;'
        );

        const indicator = new St.Bin({ style_class: 'recording-indicator' });
        indicator.set_style(
            'background-color: #ff7800; ' +
            'border-radius: 6px; ' +
            'width: 12px; ' +
            'height: 12px; ' +
            'margin-right: 8px;'
        );

        this._overlayLabel = new St.Label({
            text: 'Recording...',
            style_class: 'recording-label',
        });
        this._overlayLabel.set_style(
            'color: white; ' +
            'font-weight: bold; ' +
            'font-size: 14px;'
        );

        this._overlayWidget.add_child(indicator);
        this._overlayWidget.add_child(this._overlayLabel);
        this._overlayWidget.visible = false;

        Main.uiGroup.add_child(this._overlayWidget);
        log('[DesktopState] Overlay widget created');
    }

    destroyOverlay(): void {
        if (this._overlayWidget) {
            Main.uiGroup.remove_child(this._overlayWidget);
            this._overlayWidget.destroy();
            this._overlayWidget = null;
            this._overlayLabel = null;
            log('[DesktopState] Overlay widget destroyed');
        }
    }

    // Signal handlers
    private _onWorkspaceChanged(): void {
        const newIndex = this._workspaceManager.get_active_workspace_index();
        const total = this._workspaceManager.n_workspaces;

        log(`[DesktopState] Workspace changed: ${newIndex} / ${total}`);

        if (this._impl) {
            this._impl.emit_signal('WorkspaceChanged', GLib.Variant.new('(ii)', [newIndex, total]));
        }
    }

    private _onWorkspacesCountChanged(): void {
        const total = this._workspaceManager.n_workspaces;

        log(`[DesktopState] Workspaces count changed: ${total}`);

        if (this._impl) {
            this._impl.emit_property_changed('TotalWorkspaces', GLib.Variant.new_int32(total));
        }
    }
}

/**
 * Main extension class exported to GNOME Shell.
 */
export default class DesktopStateExtension extends Extension {
    private _service: DesktopStateService | null = null;
    private _ownerId: number | null = null;
    private _exportedObject: DBusExportedObject | null = null;

    enable(): void {
        log('[DesktopState] Extension enabling...');

        this._service = new DesktopStateService();

        // Own D-Bus name
        this._ownerId = Gio.bus_own_name(
            Gio.BusType.SESSION,
            'org.olbrasoft.Desktop',
            Gio.BusNameOwnerFlags.NONE,
            this._onBusAcquired.bind(this),
            this._onNameAcquired.bind(this),
            this._onNameLost.bind(this)
        );

        log('[DesktopState] Extension enabled');
    }

    disable(): void {
        log('[DesktopState] Extension disabling...');

        // Destroy service first
        if (this._service) {
            this._service._impl = null;
            this._service.destroy();
            this._service = null;
        }

        // Unexport D-Bus object
        if (this._exportedObject) {
            this._exportedObject.unexport();
            this._exportedObject = null;
        }

        // Unown D-Bus name last
        if (this._ownerId) {
            Gio.bus_unown_name(this._ownerId);
            this._ownerId = null;
        }

        log('[DesktopState] Extension disabled');
    }

    private _onBusAcquired(connection: unknown, name: string): void {
        log(`[DesktopState] Bus acquired: ${name}`);

        try {
            // Wrap service with D-Bus exported object
            this._exportedObject = Gio.DBusExportedObject.wrapJSObject(
                INTERFACE_XML,
                this._service
            ) as DBusExportedObject;

            if (this._service) {
                this._service._impl = this._exportedObject;
            }

            // Export on D-Bus
            this._exportedObject.export(connection, '/org/olbrasoft/Desktop');

            log('[DesktopState] D-Bus object exported at /org/olbrasoft/Desktop');
        } catch (e) {
            logError(e as Error, '[DesktopState] Failed to export D-Bus object');
        }
    }

    private _onNameAcquired(_connection: unknown, name: string): void {
        log(`[DesktopState] Name acquired: ${name}`);
        log('[DesktopState] Service is now available on D-Bus!');
    }

    private _onNameLost(_connection: unknown, name: string): void {
        log(`[DesktopState] Name lost: ${name}`);
        logError(new Error('Could not acquire D-Bus name'), '[DesktopState]');
    }
}
