namespace Torlando.SquadTracker.SquadInterface
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

        public class PlayerSortInfo
        {
            public PlayerSortInfo(string accountName, Character character, uint subgroup, byte role, bool self, bool inInstance)
            {
                AccountName = accountName;
                Character = character;
                Subgroup = subgroup;
                Role = role;
                IsSelf = self;
                IsInInstance = inInstance;
            }

            public string AccountName { get; set; }
            public Character Character { get; set; }
            public uint Subgroup { get; set; }
            public byte Role { get; set; }
            public bool IsSelf { get; set; }
            public bool IsInInstance { get; set; }
        }

        public static int Compare(PlayerSortInfo player1, PlayerSortInfo player2)
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

                        if (player1.Character != null && player2.Character != null)
                        {
                            if (player1.Character.Profession == player2.Character.Profession)
                            {
                                if (player1.Character.Specialization == player2.Character.Specialization)
                                {
                                    string p1name = player1.Character.Name;
                                    string p2name = player2.Character.Name;

                                    return p1name.CompareTo(p2name);
                                }

                                return player2.Character.Specialization.CompareTo(player1.Character.Specialization);
                            }

                            int p1 = ProfessionCodeOrder(player1.Character.Profession);
                            int p2 = ProfessionCodeOrder(player2.Character.Profession);
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

        public static int Compare(Player p1, Player p2)
        {
            PlayerSortInfo i1 = new PlayerSortInfo(p1.AccountName, p1.CurrentCharacter, p1.Subgroup, p1.Role, p1.IsSelf, p1.IsInInstance);
            PlayerSortInfo i2 = new PlayerSortInfo(p2.AccountName, p2.CurrentCharacter, p2.Subgroup, p2.Role, p2.IsSelf, p2.IsInInstance);
            return Compare(i1, i2);
        }
    }
}
