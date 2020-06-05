﻿namespace Vocup.Models
{
    public enum PracticeMode
    {
        /// <summary>
        ///     The word is displayed in the mother tongue and the user has to enter the word in the foreign language.
        /// </summary>
        AskForForeignLang = 1,

        /// <summary>
        ///     The word is displayed in the foreign language and the user has to enter the word in the mother tongue.
        /// </summary>
        AskForMotherTongue = 2
    }

    public static class PracticeModeHelper
    {
        public static bool IsValid(this PracticeMode mode)
        {
            var imode = (int) mode;
            return imode >= 1 && imode <= 2;
        }
    }
}