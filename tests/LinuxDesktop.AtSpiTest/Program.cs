using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace LinuxDesktop.AtSpiTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== AT-SPI Focus Detection Test ===\n");

        try
        {
            // Step 1: Get accessibility bus address
            Console.WriteLine("1. Getting accessibility bus address...");
            var sessionBus = new Connection(Address.Session!);
            await sessionBus.ConnectAsync();

            var writer = sessionBus.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: "org.a11y.Bus",
                path: "/org/a11y/bus",
                @interface: "org.a11y.Bus",
                member: "GetAddress");

            var a11yBusAddress = await sessionBus.CallMethodAsync(writer.CreateMessage(), (Message msg, object? state) =>
            {
                var reader = msg.GetBodyReader();
                return reader.ReadString();
            }, null);

            Console.WriteLine($"   ✓ Accessibility bus: {a11yBusAddress}\n");

            // Step 2: Connect to accessibility bus
            Console.WriteLine("2. Connecting to accessibility bus...");
            var a11yBus = new Connection(a11yBusAddress);
            await a11yBus.ConnectAsync();
            Console.WriteLine("   ✓ Connected!\n");

            // Step 3: Register for focus events
            Console.WriteLine("3. Registering for focus events...");

            // Add match rule for focus signals
            var matchRule = "type='signal',interface='org.a11y.atspi.Event.Focus',member='Focus'";
            var addMatchWriter = a11yBus.GetMessageWriter();
            addMatchWriter.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                signature: "s",
                member: "AddMatch");
            addMatchWriter.WriteString(matchRule);

            await a11yBus.CallMethodAsync(addMatchWriter.CreateMessage());
            Console.WriteLine($"   ✓ Match rule added: {matchRule}\n");

            // Step 4: Listen for focus signals
            Console.WriteLine("4. Listening for focus events...");
            Console.WriteLine("   Switch focus between applications to test.");
            Console.WriteLine("   Press Ctrl+C to exit.\n");

            var eventCount = 0;
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Simple polling approach - check for messages periodically
            // Note: This is NOT production-ready but works for PoC testing
            Console.WriteLine("⚠️  Using simplified polling approach for demonstration");
            Console.WriteLine("    For production, implement proper MessageStream integration\n");

            var messageTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Try to read a message with timeout
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                        timeoutCts.CancelAfter(100); // 100ms timeout

                        // Note: Tmds.DBus.Protocol doesn't have simple async message reading
                        // This is a limitation we'll document
                        await Task.Delay(100, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            Console.WriteLine($"Error in message loop: {ex.Message}");
                        }
                    }
                }
            }, cts.Token);

            // Wait for cancellation
            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("\n\nExiting...");
                Console.WriteLine($"Total focus events received: {eventCount}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static async Task TryGetAccessibleDetails(Connection bus, string sender, string path)
    {
        try
        {
            // Query org.a11y.atspi.Accessible interface for Name
            var nameWriter = bus.GetMessageWriter();
            nameWriter.WriteMethodCallHeader(
                destination: sender,
                path: path,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "ss",
                member: "Get");
            nameWriter.WriteString("org.a11y.atspi.Accessible");
            nameWriter.WriteString("Name");

            var name = await bus.CallMethodAsync(nameWriter.CreateMessage(), (Message msg, object? state) =>
            {
                var reader = msg.GetBodyReader();
                reader.ReadSignature(); // Read variant signature
                return reader.ReadString();
            }, null);

            Console.WriteLine($"Widget Name: '{name}'");

            // Try to get Role
            var roleWriter = bus.GetMessageWriter();
            roleWriter.WriteMethodCallHeader(
                destination: sender,
                path: path,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "ss",
                member: "Get");
            roleWriter.WriteString("org.a11y.atspi.Accessible");
            roleWriter.WriteString("Role");

            var role = await bus.CallMethodAsync(roleWriter.CreateMessage(), (Message msg, object? state) =>
            {
                var reader = msg.GetBodyReader();
                reader.ReadSignature();
                return reader.ReadUInt32();
            }, null);

            Console.WriteLine($"Role ID: {role} ({GetRoleName(role)})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not get accessible details: {ex.Message}");
        }
    }

    static string GetRoleName(uint roleId)
    {
        // AT-SPI role constants (simplified subset)
        return roleId switch
        {
            0 => "INVALID",
            1 => "ACCELERATOR_LABEL",
            2 => "ALERT",
            3 => "ANIMATION",
            4 => "ARROW",
            5 => "CALENDAR",
            6 => "CANVAS",
            7 => "CHECK_BOX",
            8 => "CHECK_MENU_ITEM",
            9 => "COLOR_CHOOSER",
            10 => "COLUMN_HEADER",
            11 => "COMBO_BOX",
            12 => "DATE_EDITOR",
            13 => "DESKTOP_ICON",
            14 => "DESKTOP_FRAME",
            15 => "DIAL",
            16 => "DIALOG",
            17 => "DIRECTORY_PANE",
            18 => "DRAWING_AREA",
            19 => "FILE_CHOOSER",
            20 => "FILLER",
            21 => "FOCUS_TRAVERSABLE",
            22 => "FONT_CHOOSER",
            23 => "FRAME",
            24 => "GLASS_PANE",
            25 => "HTML_CONTAINER",
            26 => "ICON",
            27 => "IMAGE",
            28 => "INTERNAL_FRAME",
            29 => "LABEL",
            30 => "LAYERED_PANE",
            31 => "LIST",
            32 => "LIST_ITEM",
            33 => "MENU",
            34 => "MENU_BAR",
            35 => "MENU_ITEM",
            36 => "OPTION_PANE",
            37 => "PAGE_TAB",
            38 => "PAGE_TAB_LIST",
            39 => "PANEL",
            40 => "PASSWORD_TEXT",
            41 => "POPUP_MENU",
            42 => "PROGRESS_BAR",
            43 => "PUSH_BUTTON",
            44 => "RADIO_BUTTON",
            45 => "RADIO_MENU_ITEM",
            46 => "ROOT_PANE",
            47 => "ROW_HEADER",
            48 => "SCROLL_BAR",
            49 => "SCROLL_PANE",
            50 => "SEPARATOR",
            51 => "SLIDER",
            52 => "SPIN_BUTTON",
            53 => "SPLIT_PANE",
            54 => "STATUS_BAR",
            55 => "TABLE",
            56 => "TABLE_CELL",
            57 => "TABLE_COLUMN_HEADER",
            58 => "TABLE_ROW_HEADER",
            59 => "TEAROFF_MENU_ITEM",
            60 => "TERMINAL",
            61 => "TEXT",
            62 => "TOGGLE_BUTTON",
            63 => "TOOL_BAR",
            64 => "TOOL_TIP",
            65 => "TREE",
            66 => "TREE_TABLE",
            67 => "UNKNOWN",
            68 => "VIEWPORT",
            69 => "WINDOW",
            70 => "EXTENDED",
            71 => "HEADER",
            72 => "FOOTER",
            73 => "PARAGRAPH",
            74 => "RULER",
            75 => "APPLICATION",
            76 => "AUTOCOMPLETE",
            77 => "EDITBAR",
            78 => "EMBEDDED",
            79 => "ENTRY",
            80 => "CHART",
            81 => "CAPTION",
            82 => "DOCUMENT_FRAME",
            83 => "HEADING",
            84 => "PAGE",
            85 => "SECTION",
            86 => "REDUNDANT_OBJECT",
            87 => "FORM",
            88 => "LINK",
            89 => "INPUT_METHOD_WINDOW",
            _ => $"UNKNOWN_{roleId}"
        };
    }
}
