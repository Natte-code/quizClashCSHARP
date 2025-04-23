using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuizClashCS
{
    // Utility functions
    public static class Utils
    {
        public static void ClearScreen() => Console.Clear();

        public static void PrintLogo(List<string> logo, int delay = 150)
        {
            foreach (var line in logo)
            {
                Console.WriteLine(line);
                Thread.Sleep(delay);
            }
        }
    }

    // Classes
    public class Sword
    {
        public string Name { get; }
        public int Damage { get; }
        public Sword(string name, int damage)
        {
            Name = name.ToLower();
            Damage = damage;
        }
        public override string ToString() => $"Sword(Name='{Name}', Damage={Damage})";
    }

    public class Character
    {
        public string Name { get; }
        public int Health { get; set; }
        public int Coins { get; set; } = 5;
        public int Totems { get; set; } = 0;
        public int NormalPotionCount { get; set; } = 2;
        public int EpicPotionCount { get; set; } = 1;
        public Dictionary<string, int> SwordInventory { get; }
        public DateTime LastHealTime { get; set; }

        public Character(string name, int health)
        {
            Name = name;
            Health = health;
            SwordInventory = new Dictionary<string, int>();
            // Inventory starts with a "träsvärd"
            AddSwordToInventory(new Sword("träsvärd", 10), silent: true);
        }

        public void AddSwordToInventory(Sword sword, bool silent = false)
        {
            if (!SwordInventory.ContainsKey(sword.Name))
            {
                SwordInventory[sword.Name] = sword.Damage;
                if (!silent)
                    PrintSuccess($"Du har låst upp {sword.Name.Capitalize()}!");
            }
            else
            {
                if (!silent)
                    PrintError($"Du har redan {sword.Name.Capitalize()} i inventoryt.");
            }
        }

        public int Attack(string weaponName)
        {
            weaponName = weaponName.ToLower().Trim();
            if (!SwordInventory.ContainsKey(weaponName))
            {
                var available = string.Join(", ", SwordInventory.Keys);
                throw new ArgumentException($"Invalid weapon: '{weaponName}'. Available swords: {available}");
            }
            var baseDamage = SwordInventory[weaponName];
            if (IsCriticalHit())
            {
                int critDamage = (int)(baseDamage * 2.5);
                Console.WriteLine($"\nCRITICAL HIT! {critDamage} damage!");
                return critDamage;
            }
            return baseDamage;
        }

        public int Heal(string potionType)
        {
            var now = DateTime.Now;
            if ((now - LastHealTime).TotalSeconds < 5)
            {
                Console.WriteLine("Wait 5 seconds between potions!");
                return 0;
            }
            if (potionType == "normal" && NormalPotionCount > 0)
            {
                NormalPotionCount--;
                LastHealTime = now;
                return 50;
            }
            else if (potionType == "epic" && EpicPotionCount > 0)
            {
                EpicPotionCount--;
                LastHealTime = now;
                return 100;
            }
            Console.WriteLine("No potions left!");
            return 0;
        }

        public bool Block() => new Random().NextDouble() < 0.3;

        public bool UseTotem()
        {
            if (Totems > 0)
            {
                Totems--;
                Health = 100;
                Console.WriteLine("\n=== TOTEM USED! HP RESTORED TO 100 ===");
                return true;
            }
            return false;
        }

        public void AddCoins(int amount)
        {
            Coins += amount;
            Console.WriteLine($"Earned {amount} coins! Total: {Coins}");
        }

        public void AddCoinsRandom(int amount)
        {
            Coins += amount; // In Python code random was not used effectively.
            Console.WriteLine($"Du fick {amount} coins");
        }

        private bool IsCriticalHit() => new Random().NextDouble() < 0.15;
    }

    public class Boss
    {
        public string Name { get; }
        public int Health { get; set; }
        public int MaxHealth { get; }
        public int MinDamage { get; }
        public int MaxDamage { get; }
        public int Regeneration { get; }
        public bool IsRegenerating { get; set; }
        private CancellationTokenSource regenCancellation;

        public Boss(string name, int health, int minDamage, int maxDamage, int regen)
        {
            Name = name;
            Health = health;
            MaxHealth = health;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            Regeneration = regen;
        }

        public void StartRegen()
        {
            if (!IsRegenerating)
            {
                IsRegenerating = true;
                regenCancellation = new CancellationTokenSource();
                Task.Run(() => RegenerateHealth(regenCancellation.Token));
            }
        }

        private void RegenerateHealth(CancellationToken token)
        {
            while (IsRegenerating && Health > 0 && !token.IsCancellationRequested)
            {
                Thread.Sleep(17500);
                Health = Math.Min(MaxHealth, Health + Regeneration);
                Console.WriteLine($"\nBOSS REGENERATES +{Regeneration} HP!");
            }
        }

        public void StopRegen()
        {
            IsRegenerating = false;
            regenCancellation?.Cancel();
        }

        public int Attack()
        {
            var rnd = new Random();
            return rnd.Next(MinDamage, MaxDamage + 1);
        }
    }

    public class Teacher
    {
        public string Name { get; }
        public int Health { get; set; }
        public int MinDamage { get; }
        public int MaxDamage { get; }

        public Teacher(string name, int health, int minDamage, int maxDamage)
        {
            Name = name;
            Health = health;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
        }

        public int Attack()
        {
            var rnd = new Random();
            return rnd.Next(MinDamage, MaxDamage + 1);
        }
    }

    // Extension method for capitalizing string
    public static class StringExtensions
    {
        public static string Capitalize(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }

    // Combat loop implementation
    public static class Combat
    {
        public static bool CombatLoop(Character player, dynamic enemy)
        {
            // If enemy is Boss start regen
            if (enemy is Boss boss)
            {
                boss.StartRegen();
            }

            while (player.Health > 0 && enemy.Health > 0)
            {
                Utils.ClearScreen();
                Console.WriteLine($"=== {enemy.Name} === HP: {enemy.Health}\n");
                Console.WriteLine($"{player.Name}: HP: {player.Health} | Coins: {player.Coins}");
                Console.WriteLine($"Potions: Normal({player.NormalPotionCount}) Epic({player.EpicPotionCount})");
                Console.WriteLine($"Totems: {player.Totems}\n");
                Console.WriteLine("Available swords: " + string.Join(", ", player.SwordInventory.Keys));
                Console.WriteLine("\nChoose action:");
                Console.WriteLine("1. Attack\n2. Heal\n3. Wait");
                Console.Write("\nYour choice: ");
                string choice = Console.ReadLine().ToLower();

                if (choice == "1" || choice == "attack")
                {
                    Utils.ClearScreen();
                    Console.WriteLine($"=== {enemy.Name} === HP: {enemy.Health}");
                    Console.WriteLine("Your swords: " + string.Join(", ", player.SwordInventory.Keys));
                    Console.Write("\nChoose weapon: ");
                    string weapon = Console.ReadLine().ToLower().Trim();

                    try
                    {
                        int damage = player.Attack(weapon);
                        enemy.Health -= damage;
                        Console.WriteLine($"\nYou attack with {weapon} and deal {damage} damage!");
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine($"\n{e.Message}");
                        Console.WriteLine("Press ENTER to continue...");
                        Console.ReadLine();
                        continue;
                    }
                }
                else if (choice == "2" || choice == "heal")
                {
                    Utils.ClearScreen();
                    Console.WriteLine($"=== {enemy.Name} === HP: {enemy.Health}");
                    Console.Write("Choose potion type (normal/epic): ");
                    string healType = Console.ReadLine().ToLower();
                    int healAmount = player.Heal(healType);
                    if (healAmount > 0)
                    {
                        player.Health = Math.Min(100, player.Health + healAmount);
                        Console.WriteLine($"\n? Healed {healAmount} HP!");
                    }
                }
                else if (choice == "3" || choice == "wait")
                {
                    Console.WriteLine("\nYou wait and gather your strength...");
                }
                else
                {
                    Console.WriteLine("\nInvalid choice! You lose your turn...");
                }

                Thread.Sleep(1500);
                if (enemy.Health <= 0)
                {
                    int reward = new Random().Next(10, 21);
                    player.AddCoins(reward);
                    Console.WriteLine($"\n? {enemy.Name} defeated! ?");
                    Console.WriteLine("Tip: Go and open a chest in the chest room. Normal chests: 5 coins. Epic 15.");
                    Console.WriteLine("Press ENTER to continue...");
                    Console.ReadLine();
                    return true;
                }

                // Enemy attacks
                int enemyDamage = enemy.Attack();
                if (player.Block())
                {
                    Console.WriteLine("\n?? You blocked the attack! ??");
                }
                else
                {
                    player.Health -= enemyDamage;
                    Console.WriteLine($"\n?? {enemy.Name} attacks and deals {enemyDamage} damage! ??");
                }

                if (player.Health <= 0)
                {
                    if (player.UseTotem())
                    {
                        continue;
                    }
                    Console.WriteLine("\n?? GAME OVER! ??");
                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();
                    Endings.End1();
                    return false;
                }
                Thread.Sleep(2000);
            }
            return false;
        }
    }

    // Helper functions for colored outputs and confirmations
    public static class Helper
    {
        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? " + message + " ?");
            Console.ResetColor();
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("? " + message + " ?");
            Console.ResetColor();
        }

        public static bool ConfirmPurchase(int cost, string boxType)
        {
            Utils.ClearScreen();
            Console.WriteLine($"Vill du öppna en {boxType} lootbox för {cost} coins? (ja/nej)");
            Console.Write("> ");
            return Console.ReadLine().ToLower() == "ja";
        }
    }

    // Simple lootbox system
    public static class Lootbox
    {
        public static void OpenLootbox(Dictionary<string, Action<Character>> lootpool, Character player, int cost, string boxType)
        {
            Utils.ClearScreen();
            if (player.Coins < cost)
            {
                Helper.PrintError($"Du behöver {cost} coins för en {boxType} lootbox!");
                Thread.Sleep(2000);
                return;
            }
            player.Coins -= cost;
            // Choose a random loot item from pool
            var rnd = new Random();
            var chosenItem = lootpool.Keys.ElementAt(rnd.Next(lootpool.Count));
            lootpool[chosenItem](player);
            if (chosenItem.Contains("potion"))
            {
                string potionType = chosenItem.Split('_')[0];
                Helper.PrintSuccess($"Du fick 1 {potionType} potion!");
            }
            else
            {
                Helper.PrintSuccess($"Du fick {chosenItem.Capitalize()}!");
            }
            Thread.Sleep(2000);
        }
    }

    // Endings
    public static class Endings
    {
        public static void End1()
        {
            Utils.ClearScreen();
            Console.WriteLine(@"
 __   __            ____  _          _   _ 
 \ \ / /__  _   _  |  _ \(_) ___  __| | | |
  \ V / _ \| | | | | | | | |/ _ \/ _` | | |
   | | (_) | |_| | | |_| | |  __/ (_| | |_|
   |_|\___/ \__,_| |____/|_|\___|\__,_| (_)
    
    --Tack för du har spelat Quiz Clash--
    --Spela igen för hela slutet--
    --Slut 1 av 4, (Bad ending)--
");
            Environment.Exit(0);
        }

        public static void End2()
        {
            Utils.ClearScreen();
            Console.WriteLine(@"
__   __                     _       _ 
\ \ / /__  _   _  __      _(_)_ __ | |
 \ V / _ \| | | | \ \ /\ / / | '_ \| |
  | | (_) | |_| |  \ V  V /| | | | |_|
  |_|\___/ \__,_|   \_/\_/ |_|_| |_(_)
    
    --Tack för du har spelat Quiz Clash--
    --Gjord av Nathaniel, Felix och Elliot--
    --Slut 2 av 4, (Good ending)--
");
            Environment.Exit(0);
        }

        public static void End3()
        {
            Utils.ClearScreen();
            Console.WriteLine(@"
    _     __           _     _       
   / \   / _|_ __ __ _(_) __| |      
  / _ \ | |_| '__/ _` | |/ _` |      
 / ___ \|  _| | | (_| | | (_| |_ _ _ 
/_/   \_\_| |_|  \__,_|_|\__,_(_|_|_)
    
    --Tack för du har spelat Quiz Clash--
    --Spela igen för hela slutet--
    --Slut 3 av 4, (afraid ending)--
");
            Environment.Exit(0);
        }

        public static void End4()
        {
            Utils.ClearScreen();
            Console.WriteLine(@"
                        _    __   _         _   
 _ __     __ _    ___  (_)  / _| (_)  ___  | |_ 
| '_ \   / _` |  / __| | | | |_  | | / __| | __|
| |_) | | (_| | | (__  | | |  _| | | \__ \ | |_ 
| .__/   \__,_|  \___| |_| |_|   |_| |___/  \__|
|_|
    --Tack för du har spelat Quiz Clash--
    --Spela igen för hela slutet--
    --Slut 4 av 4, (pacifist ending)--
");
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }

    // Question functions for teachers and boss
    public static class Questions
    {
        // Global counter to track teacher interactions
        public static int TeacherInteraction = 0;

        public static void JohannaQuestion(Character player, Teacher teacher)
        {
            Utils.ClearScreen();
            Console.WriteLine("Hur bra är du på matte då?");
            Thread.Sleep(2000);
            Utils.ClearScreen();

            var qAndA = new List<(string, string)>
            {
                ("Vad är 15 + 27?", "42"),
                ("Vad är 12 × 9?", "108"),
                ("Lös ekvationen: ? + 7 = 20", "13"),
                ("Vad är arean av en rektangel med längd 5 cm och bredd 3 cm?", "15 cm2"),
                ("Vad är medelvärdet av 5, 8, 12 och 20?", "11,25"),
                // add extra questions if needed...
            };
            // Randomly choose 5 questions
            var rnd = new Random();
            var selected = qAndA.OrderBy(x => rnd.Next()).Take(5).ToList();
            int correctCount = 0;
            TeacherInteraction++;
            foreach (var (question, correctAnswer) in selected)
            {
                Console.WriteLine($"Fråga: {question}");
                Console.Write("Ditt svar: ");
                string answer = Console.ReadLine().Trim().ToLower();
                if (answer == correctAnswer.ToLower())
                {
                    Console.WriteLine("Rätt!\n");
                    correctCount++;
                }
                else
                {
                    Console.WriteLine("Fel!");
                    Thread.Sleep(1000);
                    Console.WriteLine("Nu ska vi slåss >:)");
                    Thread.Sleep(1000);
                    Combat.CombatLoop(player, teacher);
                    return;
                }
            }
            if (correctCount == 5)
            {
                Utils.ClearScreen();
                Console.WriteLine("Du va bra på matte, kull för dig");
                Thread.Sleep(2000);
                player.AddCoinsRandom(15);
                teacher.Health = 0;
            }
        }

        public static void RonjaQuestion(Character player, Teacher teacher)
        {
            Utils.ClearScreen();
            Console.WriteLine("How good are you in English?");
            Thread.Sleep(2000);
            Utils.ClearScreen();

            var qAndA = new List<(string, string)>
            {
                ("What is the capital of the United Kingdom?", "London"),
                ("Who wrote the play 'Romeo and Juliet'?", "William Shakespeare"),
                ("Which word is a synonym for 'happy'?", "joyful"),
                ("What is the opposite of 'increase'?", "decrease"),
                ("What is the past tense of 'run'?", "ran"),
                // add more if needed...
            };
            var rnd = new Random();
            var selected = qAndA.OrderBy(x => rnd.Next()).Take(5).ToList();
            int correctCount = 0;
            TeacherInteraction++;
            foreach (var (question, correctAnswer) in selected)
            {
                Console.WriteLine($"Fråga: {question}");
                Console.Write("Ditt svar: ");
                string answer = Console.ReadLine().Trim().ToLower();
                if (answer == correctAnswer.ToLower())
                {
                    Console.WriteLine("Rätt!\n");
                    correctCount++;
                }
                else
                {
                    Console.WriteLine("Wrong! Thats the wrong answer, now DIE");
                    Thread.Sleep(2000);
                    Combat.CombatLoop(player, teacher);
                    return;
                }
            }
            if (correctCount == 5)
            {
                Utils.ClearScreen();
                Console.WriteLine("Wow, your English is impressive. Move on");
                Thread.Sleep(2000);
                player.AddCoinsRandom(15);
                teacher.Health = 0;
            }
        }

        // Similar question functions (henrik, victor, david, mirrela, lars boss) can be created.
        // For brevity, we include only two here.
    }

    // Main game flow
    class Program
    {
        // Predefined sword instances
        static Sword Pie = new Sword("pie", 3);
        static Sword Jarnsvard = new Sword("järnsvärd", 20);
        static Sword Katana = new Sword("katana", 15);
        static Sword Dagger = new Sword("dagger", 17);
        static Sword Pinne = new Sword("pinne", 10);
        static Sword Kukri = new Sword("kukri", 25);
        static Sword BattleAxe = new Sword("battle_axe", 35);
        static Sword Lightsaber = new Sword("lightsaber", 50);
        static Sword Stekpanna = new Sword("stekpanna", 69);
        static Sword Skibidi = new Sword("skibidi", 1523048957); // debug, smaller number for C#
        static Sword Trasvard = new Sword("träsvärd", 10);

        // Predefined teachers and boss
        static Teacher teacher1 = new Teacher("Johanna", 100, 1, 10);
        static Teacher teacher2 = new Teacher("Ronja", 110, 5, 15);
        static Boss finalBoss = new Boss("Lars", 500, 20, 50, 20);

        static void StartScreen()
        {
            Utils.ClearScreen();
            Console.WriteLine("\nvälkommen till ?Quiz Clash?\n");
            Thread.Sleep(1000);
            Console.WriteLine("....");
            Thread.Sleep(1000);
            var logo = new List<string>
            {
                " ________  ___  ___  ___  ________                    ",
                "|\\   __  \\|\\  \\|\\  \\|\\  \\|\\_____  \\                   ",
                "\\ \\  \\|\\  \\ \\  \\\\\\  \\ \\  \\\\|___/  /|                  ",
                " \\ \\  \\\\\\  \\ \\  \\\\\\  \\ \\  \\   /  / /                  ",
                "  \\ \\  \\\\\\  \\ \\  \\\\\\  \\ \\  \\ /  /_/__                 ",
                "   \\ \\_____  \\ \\_______\\ \\__\\\\________\\               ",
                "    \\|___| \\__\\|_______|\\|__|\\|_______|              ",
                "          \\|__|                                       ",
                "                                                     ",
                " ________  ___       ________  ________  ___  ___    ",
                "|\\   ____\\|\\  \\     |\\   __  \\|\\   ____\\|\\  \\|\\  \\   ",
                "\\ \\  \\___|\\ \\  \\    \\ \\  \\|\\  \\ \\  \\___|\\ \\  \\\\\\  \\  ",
                " \\ \\  \\    \\ \\  \\    \\ \\   __  \\ \\_____  \\ \\   __  \\ ",
                "  \\ \\  \\____\\ \\  \\____\\ \\  \\ \\  \\|____|\\  \\ \\  \\ \\  \\",
                "   \\ \\_______\\ \\_______\\ \\__\\ \\__\\____\\_\\  \\ \\__\\ \\__\\",
                "    \\|_______|\\|_______|\\|__|\\|__|\\_________\\|__|\\|__|",
                "                                 \\|_________|         "
            };
            Utils.PrintLogo(logo);
            Thread.Sleep(1000);
            Console.WriteLine(@"
Hur man spelar:
   Rör dig med WASD
   Samla mynt, öppna lådor och upptäck klassrum. 
   Svara på lärarens frågor. Fel svar leder till turordningsbaserade strider.
   Håll terminalen i FULL SCREEN!
   Håll koll på ditt inventory (går ej att släppa saker).
   Öppna lådor för att få nya vapen och potions.
Lycka till!!
");
            Console.WriteLine("\nTryck på ENTER knappen för att starta spelet!");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            StartScreen();
            Console.Write("Ange ditt namn: ");
            string playerName = Console.ReadLine();
            var player = new Character(playerName, 100);

            // Pre-add some swords to loot pools (simulate unlocks)
            // For simplicity these two loot pools are created as dictionaries
            var lootpoolNormal = new Dictionary<string, Action<Character>>
            {
                { "kukri", p => p.AddSwordToInventory(Kukri) },
                { "järnsvärd", p => p.AddSwordToInventory(Jarnsvard) },
                { "normal_potion", p => p.NormalPotionCount++ },
                { "katana", p => p.AddSwordToInventory(Katana) },
                { "dagger", p => p.AddSwordToInventory(Dagger) },
                { "pinne", p => p.AddSwordToInventory(Pinne) },
            };

            var lootpoolEpic = new Dictionary<string, Action<Character>>
            {
                { "epic_potion", p => p.EpicPotionCount++ },
                { "battle_axe", p => p.AddSwordToInventory(BattleAxe) },
                { "totem", p => p.Totems++ },
                { "lightsaber", p => p.AddSwordToInventory(Lightsaber) },
                { "stekpanna", p => p.AddSwordToInventory(Stekpanna) },
            };

            // The game loop: for demonstration we show a simple menu to choose which teacher challenge to attempt.
            bool playing = true;
            while (playing)
            {
                Utils.ClearScreen();
                Console.WriteLine("Välj en lärare att utmana:");
                Console.WriteLine("1. Johanna (Matte)");
                Console.WriteLine("2. Ronja (English)");
                Console.WriteLine("3. Slåss mot final boss: Lars");
                Console.WriteLine("4. Öppna Normal lootbox (kostar 5 coins)");
                Console.WriteLine("5. Öppna Epic lootbox (kostar 15 coins)");
                Console.WriteLine("6. Visa status");
                Console.WriteLine("7. Avsluta spelet");
                Console.Write("Val: ");
                string input = Console.ReadLine().Trim();
                switch (input)
                {
                    case "1":
                        if (teacher1.Health > 0)
                        {
                            Questions.JohannaQuestion(player, teacher1);
                        }
                        else
                        {
                            Console.WriteLine("Johanna är redan besegrad.");
                            Thread.Sleep(1500);
                        }
                        break;
                    case "2":
                        if (teacher2.Health > 0)
                        {
                            Questions.RonjaQuestion(player, teacher2);
                        }
                        else
                        {
                            Console.WriteLine("Ronja är redan besegrad.");
                            Thread.Sleep(1500);
                        }
                        break;
                    case "3":
                        // Final boss challenge
                        if (finalBoss.Health > 0)
                        {
                            Console.WriteLine("Du möter Lars, din final boss. Svara på frågorna, annars slåss!");
                            // For simplicity we call combat directly here.
                            Combat.CombatLoop(player, finalBoss);
                        }
                        else
                        {
                            Console.WriteLine("Lars är redan besegrad.");
                            Thread.Sleep(1500);
                        }
                        break;
                    case "4":
                        Lootbox.OpenLootbox(lootpoolNormal, player, 5, "Normal chest");
                        break;
                    case "5":
                        Lootbox.OpenLootbox(lootpoolEpic, player, 15, "Epic chest");
                        break;
                    case "6":
                        // Show simple status
                        Console.WriteLine($"\nPlayer: {player.Name}\nHealth: {player.Health}\nCoins: {player.Coins}\nTotems: {player.Totems}");
                        Console.WriteLine("Inventory:");
                        foreach (var item in player.SwordInventory)
                        {
                            Console.WriteLine($" - {item.Key}: damage {item.Value}");
                        }
                        Console.WriteLine($"Normal potions: {player.NormalPotionCount}, Epic potions: {player.EpicPotionCount}");
                        Console.WriteLine("\nPress ENTER to continue...");
                        Console.ReadLine();
                        break;
                    case "7":
                        playing = false;
                        break;
                    default:
                        Console.WriteLine("Ogiltigt val!");
                        Thread.Sleep(1500);
                        break;
                }
            }

            Console.WriteLine("Tack för du har spelat Quiz Clash!");
        }
    }
}
