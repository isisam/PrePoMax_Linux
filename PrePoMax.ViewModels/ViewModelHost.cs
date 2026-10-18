// SPDX-License-Identifier: GPL-3.0-or-later
using System;
namespace PrePoMax
{
    public static class ViewModelHost
    {
        public static string ExportFileFilter;
        public static Func<bool> ConfirmMissingWorkDirectory { get; set; } = () =>
            throw new NotSupportedException("The host must confirm a missing work directory.");
    }
}
