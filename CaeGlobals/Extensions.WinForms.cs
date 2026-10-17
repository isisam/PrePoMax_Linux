#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Windows.Forms;
namespace CaeGlobals
{
    public static partial class Extensions
    {
        // Property grid items
        public static IEnumerable<GridItem> EnumerateAllItems(this PropertyGrid grid)
        {
            if (grid == null) yield break;
            // Get to root item
            GridItem start = grid.SelectedGridItem;
            while (start.Parent != null)
            {
                start = start.Parent;
            }
            //
            foreach (GridItem item in start.EnumerateAllItems())
            {
                yield return item;
            }
        }
        public static IEnumerable<GridItem> EnumerateAllItems(this GridItem item)
        {
            if (item == null) yield break;
            //
            yield return item;
            foreach (GridItem child in item.GridItems)
            {
                foreach (GridItem gc in child.EnumerateAllItems())
                {
                    yield return gc;
                }
            }
        }
        // Controls
        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }
    }
}
#endif
