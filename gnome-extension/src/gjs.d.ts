// Type declarations for GJS/GNOME Shell modules

declare module 'gi://Gio' {
    namespace Gio {
        const BusType: {
            SESSION: number;
            SYSTEM: number;
        };
        const BusNameOwnerFlags: {
            NONE: number;
        };
        function bus_own_name(
            busType: number,
            name: string,
            flags: number,
            busAcquired: (connection: unknown, name: string) => void,
            nameAcquired: (connection: unknown, name: string) => void,
            nameLost: (connection: unknown, name: string) => void
        ): number;
        function bus_unown_name(ownerId: number): void;
        const DBusExportedObject: {
            wrapJSObject(interfaceXml: string, object: unknown): DBusExportedObject;
        };
    }
    interface DBusExportedObject {
        export(connection: unknown, path: string): void;
        unexport(): void;
        emit_signal(name: string, variant: unknown): void;
        emit_property_changed(name: string, variant: unknown): void;
    }
    export default Gio;
}

declare module 'gi://GLib' {
    namespace GLib {
        const Variant: {
            ['new'](type: string, value: unknown): unknown;
            new_int32(value: number): unknown;
            new_string(value: string): unknown;
        };
        // Returns current monotonic time in microseconds
        function get_monotonic_time(): number;
    }
    export default GLib;
}

declare module 'gi://Shell' {
    namespace Shell {
        const WindowTracker: {
            get_default(): ShellWindowTracker;
        };
    }
    export default Shell;
}

// AT-SPI accessibility interface
declare module 'gi://Atspi' {
    namespace Atspi {
        function init(): number;
        function get_desktop(index: number): AtspiAccessible | null;

        const CoordType: {
            SCREEN: number;
            WINDOW: number;
            PARENT: number;
        };

        const StateType: {
            FOCUSED: number;
            SELECTED: number;
            ACTIVE: number;
            EDITABLE: number;
        };

        // AT-SPI Roles for identifying widget types
        const Role: {
            APPLICATION: number;
            FRAME: number;
            DIALOG: number;
            WINDOW: number;
            PANEL: number;
            TEXT: number;
            TERMINAL: number;
            ENTRY: number;
            PASSWORD_TEXT: number;
            COMBO_BOX: number;
            SPIN_BUTTON: number;
            DATE_EDITOR: number;
        };
    }
    export default Atspi;
}

// AT-SPI Accessible object
interface AtspiAccessible {
    get_name(): string;
    get_role(): number;
    get_role_name(): string;
    get_child_count(): number;
    get_child_at_index(index: number): AtspiAccessible | null;
    get_parent(): AtspiAccessible | null;
    get_state_set(): AtspiStateSet | null;
    get_text_iface(): AtspiText | null;
    get_component_iface(): AtspiComponent | null;
}

// AT-SPI StateSet
interface AtspiStateSet {
    contains(state: number): boolean;
}

// AT-SPI Text interface
interface AtspiText {
    get_caret_offset(): number;
    get_character_count(): number;
    get_text(start: number, end: number): string;
    get_character_extents(offset: number, coordType: number): AtspiRect;
}

// AT-SPI Component interface
interface AtspiComponent {
    get_extents(coordType: number): AtspiRect;
    get_position(coordType: number): AtspiPoint;
    get_size(): AtspiPoint;
}

// AT-SPI Rect (returned by get_character_extents)
interface AtspiRect {
    x: number;
    y: number;
    width: number;
    height: number;
}

// AT-SPI Point
interface AtspiPoint {
    x: number;
    y: number;
}

// AT-SPI Caret Event (from FocusCaretTracker)
interface AtspiCaretEvent {
    source: AtspiAccessible | null;
    detail1: number;
    detail2: number;
}

// St (Shell Toolkit) for UI widgets
declare module 'gi://St' {
    namespace St {
        class Widget {
            x: number;
            y: number;
            visible: boolean;
            destroy(): void;
            add_style_class_name(className: string): void;
            set_style(css: string): void;
        }
        class Label extends Widget {
            text: string;
            constructor(params?: { text?: string; style_class?: string });
        }
        class BoxLayout extends Widget {
            constructor(params?: { style_class?: string; vertical?: boolean });
            add_child(child: Widget): void;
        }
        class Bin extends Widget {
            constructor(params?: { style_class?: string });
            set_child(child: Widget | null): void;
        }
        const ThemeContext: {
            get_for_stage(stage: unknown): { scale_factor: number };
        };
    }
    export default St;
}

// GNOME Shell FocusCaretTracker module
// Based on: https://gitlab.gnome.org/GNOME/gnome-shell/-/blob/main/js/ui/focusCaretTracker.js
declare module 'resource:///org/gnome/shell/ui/focusCaretTracker.js' {
    export class FocusCaretTracker {
        constructor();
        connect(signal: string, callback: (tracker: unknown, event: AtspiCaretEvent) => void): number;
        disconnect(id: number): void;
        // Focus events (object:state-changed:focused, object:state-changed:selected)
        registerFocusListener(): void;
        deregisterFocusListener(): void;
        // Caret events (object:text-caret-moved) - USE THIS FOR CARET POSITION
        registerCaretListener(): void;
        deregisterCaretListener(): void;
    }
}

// FocusCaretTracker instance type
interface FocusCaretTrackerInstance {
    connect(signal: string, callback: (tracker: unknown, event: AtspiCaretEvent) => void): number;
    disconnect(id: number): void;
    registerFocusListener(): void;
    deregisterFocusListener(): void;
    registerCaretListener(): void;
    deregisterCaretListener(): void;
}

// Main module for GNOME Shell UI
declare module 'resource:///org/gnome/shell/ui/main.js' {
    export const uiGroup: {
        add_child(actor: unknown): void;
        remove_child(actor: unknown): void;
    };
    export const layoutManager: {
        monitors: Array<{ x: number; y: number; width: number; height: number }>;
        primaryIndex: number;
    };
}

// Shell types exported globally for use in extension
interface ShellWindowTracker {
    focus_app: ShellApplication | null;
    get_window_app(window: Window): ShellApplication | null;
    connect(signal: string, callback: () => void): number;
    disconnect(id: number): void;
}

interface ShellApplication {
    get_id(): string;
}

declare module 'resource:///org/gnome/shell/extensions/extension.js' {
    export class Extension {
        enable(): void;
        disable(): void;
    }
}

// GNOME Shell global object
declare const global: {
    workspace_manager: WorkspaceManager;
    display: Display;
    stage: unknown;
    get_pointer(): [number, number, unknown];
};

interface WorkspaceManager {
    get_active_workspace_index(): number;
    n_workspaces: number;
    get_workspace_by_index(index: number): Workspace | null;
    connect(signal: string, callback: () => void): number;
    disconnect(id: number): void;
}

interface Workspace {
    list_windows(): Window[];
}

interface Window {
    get_title(): string | null;
    get_wm_class(): string | null;
    get_frame_rect(): Rectangle;
    get_client_content_rect?(): Rectangle;
    skip_taskbar: boolean;
}

interface Rectangle {
    x: number;
    y: number;
    width: number;
    height: number;
}

interface Display {
    focus_window: Window | null;
    connect(signal: string, callback: () => void): number;
    disconnect(id: number): void;
}

interface Application {
    get_id(): string;
}

// GJS logging functions
declare function log(message: string): void;
declare function logError(error: Error, prefix: string): void;
