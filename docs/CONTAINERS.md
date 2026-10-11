# GroupBox and Accordion

`LabzGroupBox` is a themed WinForms `GroupBox` with rounded border and title.
Drop controls into it in the Designer just like the native GroupBox.

`LabzAccordion` is an expandable section with a keyboard-operable header.
Place children in `ContentPanel`; toggling `Expanded` hides or shows them
without removing controls. `ExpandedHeight` controls the restored height.

```csharp
var advanced = new LabzAccordion { Title = "Advanced options", ExpandedHeight = 220 };
advanced.ContentPanel.Controls.Add(new LabzTextBox { Dock = DockStyle.Top });
advanced.ExpandedChanged += (_, _) => Console.WriteLine(advanced.Expanded);
```

The header uses a native Button for Enter/Space keyboard activation.
The accordion supports independent expansion; coordinated single-open
accordion groups and animations are intentionally deferred.

Interactive Designer placement and accessibility should be checked on Windows.
