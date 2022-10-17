using System;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.SquadInterface
{
    internal static class SquadPlayerSort
    {
        private static int ProfessionCodeOrder(uint profession)
        {
            return profession switch
            {
                2 => // Warrior
                    0,
                1 => // Guardian
                    1,
                9 => // Revenant
                    2,
                4 => // Ranger
                    3,
                5 => // Thief
                    4,
                3 => // Engineer
                    5,
                8 => // Necromancer
                    6,
                6 => // Elementalist
                    7,
                7 => // Warrior
                    8,
                _ => 9
            };
        }

        public static int Compare(Player player1, Player player2)
        {
            var cmp = player1.Subgroup.CompareTo(player2.Subgroup);
            if (cmp != 0)
                return cmp;
            
            if (player1.IsInInstance != player2.IsInInstance)
                return player2.IsInInstance.CompareTo(player1.IsInInstance);
            
            if (player1.Role != player2.Role)
                return player1.Role.CompareTo(player2.Role);
            
            if (player1.IsSelf)
                return -1;
            if (player2.IsSelf)
                return 1;

            if (player1.CurrentCharacter == null || player2.CurrentCharacter == null)
                return string.Compare(player1.AccountName, player2.AccountName, StringComparison.Ordinal);

            if (player1.CurrentCharacter.Profession != player2.CurrentCharacter.Profession)
            {
                var p1 = ProfessionCodeOrder(player1.CurrentCharacter.Profession);
                var p2 = ProfessionCodeOrder(player2.CurrentCharacter.Profession);
                return p1.CompareTo(p2);
            }
            
            if (player1.CurrentCharacter.Specialization == player2.CurrentCharacter.Specialization)
                return string.Compare(player1.CurrentCharacter.Name, player2.CurrentCharacter.Name, StringComparison.Ordinal);

            return player2.CurrentCharacter.Specialization.CompareTo(player1.CurrentCharacter.Specialization);
        }

        public static int Compare(PlayerDisplay pd1, PlayerDisplay pd2)
        {
            var cmp = pd1.Subgroup.CompareTo(pd2.Subgroup);
            if (cmp != 0)
                return cmp;
            
            if (pd1.IsInInstance != pd2.IsInInstance)
                return pd2.IsInInstance.CompareTo(pd1.IsInInstance);
            
            if (pd1.Role != pd2.Role)
                return pd1.Role.CompareTo(pd2.Role);
            
            if (pd1.IsSelf)
                return -1;
            if (pd2.IsSelf)
                return 1;
            
            if (string.IsNullOrEmpty(pd1.CharacterName) || string.IsNullOrEmpty(pd2.CharacterName))
                return string.Compare(pd1.AccountName, pd2.AccountName, StringComparison.Ordinal);

            if (pd1.Profession != pd2.Profession)
            {
                var p1 = ProfessionCodeOrder(pd1.Profession);
                var p2 = ProfessionCodeOrder(pd2.Profession);
                return p1.CompareTo(p2);
            }
            
            if (pd1.Specialization != pd2.Specialization)
                return pd2.Specialization.CompareTo(pd1.Specialization);
            
            return string.Compare(pd1.CharacterName, pd2.CharacterName, StringComparison.Ordinal);
        }
    }
}
