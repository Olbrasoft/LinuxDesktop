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
