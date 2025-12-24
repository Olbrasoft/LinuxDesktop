namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// AT-SPI accessible object roles (subset of commonly used roles).
/// Full list: https://www.freedesktop.org/wiki/Accessibility/AT-SPI2/
/// </summary>
public enum AccessibleRole
{
    Unknown = 0,
    Invalid = 0,

    // Interactive elements
    Entry = 79,              // Text input field
    Terminal = 60,           // Terminal emulator
    Text = 61,               // Text widget
    PasswordText = 40,       // Password input field
    PushButton = 43,         // Button
    ToggleButton = 62,       // Toggle button
    CheckBox = 7,            // Checkbox
    RadioButton = 44,        // Radio button
    ComboBox = 11,           // Combo box/dropdown

    // Containers
    Panel = 39,              // Generic panel/container
    Frame = 23,              // Top-level window frame
    Dialog = 16,             // Dialog window
    Window = 69,             // Generic window
    Application = 75,        // Application root object

    // Structure
    MenuBar = 34,            // Menu bar
    Menu = 33,               // Menu
    MenuItem = 35,           // Menu item
    ToolBar = 63,            // Toolbar
    StatusBar = 54,          // Status bar

    // Documents
    DocumentFrame = 82,      // Document container
    Paragraph = 73,          // Paragraph of text
    Heading = 83,            // Heading
    Link = 88,               // Hyperlink

    // Lists and tables
    List = 31,               // List
    ListItem = 32,           // List item
    Table = 55,              // Table
    TableCell = 56,          // Table cell

    // Other
    Label = 29,              // Static label
    Icon = 26,               // Icon
    Image = 27,              // Image
}
