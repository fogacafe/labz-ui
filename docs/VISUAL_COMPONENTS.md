# Visual components

- `LabzBadge`: small status pill with Neutral, Primary, Success, Warning
  and Error variants.
- `LabzCard`: reusable rounded container with optional heading and border.
- `LabzSpinner`: animated loading indicator; set `Active = false` to pause.
- `LabzEmptyState`: centered heading and description for empty result views.

```csharp
var badge = new LabzBadge { Text = "Pending", Variant = LabzBadgeVariant.Warning };
var card = new LabzCard { Title = "Trade details" };
card.Controls.Add(new LabzLabel { Text = "Ready for review" });
var spinner = new LabzSpinner { Active = true };
var empty = new LabzEmptyState
{
    Heading = "No records",
    Description = "Adjust the filters and try again."
};
```

All four follow the current Labz theme. The Showcase includes live examples.
