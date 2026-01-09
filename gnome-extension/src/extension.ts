// SPDX-License-Identifier: GPL-2.0-or-later
// Desktop State Tracker - GNOME Shell Extension
// Provides D-Bus API for desktop context awareness

// Type declarations are in gjs.d.ts
/// <reference path="./gjs.d.ts" />

import Gio from 'gi://Gio';
import GLib from 'gi://GLib';
import Shell from 'gi://Shell';
import { Extension } from 'resource:///org/gnome/shell/extensions/extension.js';

// Internal types
interface GObjectWithSignals {
    connect(signal: string, callback: () => void): number;
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
      <arg type="(ii)" direction="out" name="position"/>
    </method>

    <method name="GetActiveWindowGeometry">
      <arg type="(iiii)" direction="out" name="geometry"/>
    </method>

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
 */
class DesktopStateService {
    private _workspaceManager: WorkspaceManager;
    private _display: Display;
    private _tracker: ShellWindowTracker;
    private _signalIds: SignalConnection[];
    public _impl: DBusExportedObject | null = null;

    constructor() {
        this._workspaceManager = global.workspace_manager;
        this._display = global.display;
        this._tracker = Shell.WindowTracker.get_default() as ShellWindowTracker;
        this._signalIds = [];

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
        // Disconnect all signals
        this._signalIds.forEach((signal) => {
            signal.object.disconnect(signal.id);
        });
        this._signalIds = [];

        log('[DesktopState] Service destroyed');
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
        return GLib.Variant.new('a(sss)', applications);
    }

    GetPointerPosition(): unknown {
        const [x, y] = global.get_pointer();
        log(`[DesktopState] GetPointerPosition: (${x}, ${y})`);
        return GLib.Variant.new('(ii)', [x, y]);
    }

    GetActiveWindowGeometry(): unknown {
        const window = this._display.focus_window;
        if (!window) {
            log('[DesktopState] GetActiveWindowGeometry: no focused window');
            return GLib.Variant.new('(iiii)', [0, 0, 0, 0]);
        }

        const rect = window.get_frame_rect();
        log(`[DesktopState] GetActiveWindowGeometry: (${rect.x}, ${rect.y}, ${rect.width}, ${rect.height})`);
        return GLib.Variant.new('(iiii)', [rect.x, rect.y, rect.width, rect.height]);
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

        // Emit property change signal
        if (this._impl) {
            this._impl.emit_property_changed('TotalWorkspaces', GLib.Variant.new_int32(total));
        }
    }

    private _onFocusChanged(): void {
        const window = this._display.focus_window;
        const app = this._tracker.focus_app;

        const windowTitle = window ? window.get_title() ?? '' : '';
        const appId = app ? app.get_id() : '';
        const wmClass = window ? window.get_wm_class() ?? '' : '';

        log(`[DesktopState] Focus changed: ${windowTitle} (${appId}) [${wmClass}]`);

        if (this._impl) {
            this._impl.emit_signal('FocusChanged', GLib.Variant.new('(sss)', [windowTitle, appId, wmClass]));

            // Also emit property changes
            this._impl.emit_property_changed('ActiveWindow', GLib.Variant.new_string(windowTitle));
            this._impl.emit_property_changed('ActiveApplication', GLib.Variant.new_string(appId));
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
