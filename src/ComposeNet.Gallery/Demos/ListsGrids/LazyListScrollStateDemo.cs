using System.Linq;
using AndroidX.Compose.Foundation.Lazy;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.ListsGrids;

/// <summary>
/// LazyColumn with a live readout of <see cref="LazyListState"/>'s
/// scroll-direction and visible-item properties so a reviewer can
/// scroll the list and watch the snapshot-backed values update on the
/// device in real time. Exercises #164.
/// </summary>
public static class LazyListScrollStateDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-lazy-list-scroll-state",
        CategoryId:  "lists-grids",
        Title:       "LazyListState — scroll-direction readout",
        Description: "1000-row LazyColumn with a live readout of FirstVisibleItemIndex, " +
                     "FirstVisibleItemScrollOffset, CanScrollBackward/Forward, " +
                     "LastScrolledBackward/Forward, and IsScrollInProgress.",
        Build:       () =>
        {
            var state = Compose.Remember(() => new LazyListState());
            return new Column(verticalArrangement: Arrangement.SpacedBy(4))
            {
                Modifier.Companion.FillMaxWidth(),

                new Surface
                {
                    Modifier.Companion.FillMaxWidth(),
                    new Column(verticalArrangement: Arrangement.SpacedBy(2))
                    {
                        Modifier.Companion.Padding(12),
                        new Text($"FirstVisibleItemIndex:        {state.FirstVisibleItemIndex}"),
                        new Text($"FirstVisibleItemScrollOffset: {state.FirstVisibleItemScrollOffset}"),
                        new Text($"CanScrollBackward:            {state.CanScrollBackward}"),
                        new Text($"CanScrollForward:             {state.CanScrollForward}"),
                        new Text($"LastScrolledBackward:         {state.LastScrolledBackward}"),
                        new Text($"LastScrolledForward:          {state.LastScrolledForward}"),
                        new Text($"IsScrollInProgress:           {state.IsScrollInProgress}"),
                    },
                },

                new Box
                {
                    Modifier.Companion.FillMaxWidth().Height(360),

                    new LazyColumn<int>(
                        items:       Enumerable.Range(0, 1000).ToList(),
                        itemContent: i => new Text($"Row {i:D4}"))
                    {
                        Modifier = Modifier.Companion.FillMaxSize(),
                        State    = state,
                    },
                },
            };
        });
}
