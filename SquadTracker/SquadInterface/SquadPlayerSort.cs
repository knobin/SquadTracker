using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class SquadPlayerSort
    {
        public static int ProfessionCodeOrder(uint profession)
        {
            if (profession == 2) // Warrior
                return 0;
            else if (profession == 1) // Guardian
                return 1;
            else if (profession == 9) // Revenant
                return 2;
            else if (profession == 4) // Ranger
                return 3;
            else if (profession == 5) // Thief
                return 4;
            else if (profession == 3) // Engineer
                return 5;
            else if (profession == 8) // Necromancer
                return 6;
            else if (profession == 6) // Elementalist
                return 7;
            else if (profession == 7) // Mesmer
                return 8;

            return 9;
        }

        public static int Compare(Player player1, Player player2)
        {
            var cmp = player1.Subgroup.CompareTo(player2.Subgroup);

            if (cmp == 0)
            {
                if (player1.IsInInstance == player2.IsInInstance)
                {
                    if (player1.Role == player2.Role)
                    {
                        if (player1.IsSelf)
                            return -1;
                        if (player2.IsSelf)
                            return 1;

                        if (player1.CurrentCharacter != null && player2.CurrentCharacter != null)
                        {
                            if (player1.CurrentCharacter.Profession == player2.CurrentCharacter.Profession)
                            {
                                if (player1.CurrentCharacter.Specialization == player2.CurrentCharacter.Specialization)
                                {
                                    string p1name = player1.CurrentCharacter.Name;
                                    string p2name = player2.CurrentCharacter.Name;

                                    return p1name.CompareTo(p2name);
                                }

                                return player2.CurrentCharacter.Specialization.CompareTo(player1.CurrentCharacter.Specialization);
                            }

                            int p1 = ProfessionCodeOrder(player1.CurrentCharacter.Profession);
                            int p2 = ProfessionCodeOrder(player2.CurrentCharacter.Profession);
                            return p1.CompareTo(p2);
                        }

                        string a1name = player1.AccountName.TrimStart(':');
                        string a2name = player2.AccountName.TrimStart(':');
                        return a1name.CompareTo(a2name);
                    }

                    return player1.Role.CompareTo(player2.Role);
                }

                return player2.IsInInstance.CompareTo(player1.IsInInstance);
            }

            return cmp;
        }
    }
}
