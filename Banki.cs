using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace IDZ_1_MVP_2.Banki
{
    public enum Nominal
    {
        TenRub = 10,
        FiftyRub = 50,
        HundredRub = 100,
        FiveHundredRub = 500,
        ThousandRub = 1000
    }

    public class MoneyStack
    {
        private int _count;

        public Nominal Nom { get; }
        public int Count { get => _count ; }

        private MoneyStack(Nominal nom, int count)
        {
            Nom = nom;
            _count = count;
        }

        public static bool TryCreate(int nominalValue, int count, out MoneyStack? result)
        {
            result = null;

            if (count < 0)
                return false;

            if (!Enum.IsDefined(typeof(Nominal), nominalValue))
                return false;

            Nominal nom = (Nominal)nominalValue;
            result = new MoneyStack(nom, count);
            return true;
        }

        public void Add(int count) => _count += count;

        public void Take(ref int amount)
        {
            if (_count == 0 || amount < (int)Nom)
                return;

            if (amount >= TotalValue())
            {
                amount -= TotalValue();
                _count = 0;
            }
            else
            {
                int needed = amount / (int)Nom;
                int taken = Math.Min(needed, _count);
                amount -= taken * (int)Nom;
                _count -= taken;
            }
        }

        public int TotalValue() => (int)Nom * _count;

        public MoneyStack Clone()
        {
            return new MoneyStack(Nom, _count);
        }

        public override string ToString() => $"{_count} x {Nom} руб. = {TotalValue()} руб.";
    }


    public interface ICashDispenser
    {
        void LoadCash(int nominal, int count);
        bool TryWithdraw(int amount);
        int TakeBalance();
    }

    public abstract class CashBox: ICashDispenser
    {
        public abstract void LoadCash(int nominal, int count);
        public abstract bool TryWithdraw(int amount);
        public abstract int TakeBalance();
        public override abstract string ToString();

        ~CashBox()
        {
            Console.WriteLine($"Коробка денег {this} уничтожена");
        }
    }

    public class Bankomat: CashBox
    {
        private int _id;
        private int _minLimit;
        private int _maxLimit;

        private MoneyStack[] _moneyStacks;

        private Bankomat(int id, int minLimit, int maxLimit)
        {
            _id = id;
            _minLimit = minLimit;
            _maxLimit = maxLimit;
            _moneyStacks = CreateEmptyArray();
            Console.WriteLine($"Банкомат {this} создан");
        }

        public static bool TryCreate(int id, int minLimit, int maxLimit, out Bankomat? result)
        {
            result = null;
            if (id < 0) return false;
            if (minLimit < 0) return false;
            if (maxLimit < 0) return false;
            if (minLimit > maxLimit) return false;
            result = new Bankomat(id, minLimit, maxLimit);
            return true;
        }

        public override void LoadCash(int nominal, int count)
        {
            foreach(var moneyStack in _moneyStacks)
            {
                if (nominal == (int)moneyStack.Nom)
                {
                    moneyStack.Add(count);
                    break;
                }
            }
        }

        public override bool TryWithdraw(int amount)
        {
            if (amount < _minLimit || amount > _maxLimit || amount <= 0 || amount > TakeBalance())
                return false;

            MoneyStack[] copy = _moneyStacks.Select(s => s.Clone()).ToArray();

            int remaining = amount;
            foreach (var stack in copy.Reverse())
            {
                stack.Take(ref remaining);
                if (remaining == 0) break;
            }

            if (remaining != 0)
                return false;

            _moneyStacks = copy;
            return true;
        }

        public override int TakeBalance()
        {
            int balance = 0;

            foreach (var moneyStack in _moneyStacks)
            {
                balance += moneyStack.TotalValue();
            }

            return balance;
        }

        public override string ToString()
        {
            string header = $"Id: {_id}, Min: {_minLimit}, Max: {_maxLimit}, Balance: {TakeBalance()}";
            string details = string.Join(", ", _moneyStacks.Select(s => $"{(int)s.Nom}: {s.Count}"));
            return header + "\n" + details;
        }

        ~Bankomat()
        {
            Console.WriteLine($"Банкомат {this} уничтожен");
        }

        private static MoneyStack[] CreateEmptyArray()
        {
            var nominals = Enum.GetValues(typeof(Nominal))
                               .Cast<Nominal>()
                               .OrderBy(n => (int)n)
                               .ToArray();

            var array = new MoneyStack[nominals.Length];
            for (int i = 0; i < nominals.Length; i++)
            {
                MoneyStack.TryCreate((int)nominals[i], 0, out array[i]);
            }
            return array;
        }
    }

    public class Program
    {
        static void Main()
        {
            const string CommandLoadCash = "1";
            const string CommandWithdraw = "2";
            const string CommandBalance = "3";
            const string CommandInfo = "4";
            const string CommandExit = "5";

            // Создаём банкомат с параметрами, введёнными пользователем
            Console.Write("Введите ID банкомата: ");
            int id = int.Parse(Console.ReadLine());

            Console.Write("Минимальная сумма снятия: ");
            int minLimit = int.Parse(Console.ReadLine());

            Console.Write("Максимальная сумма снятия: ");
            int maxLimit = int.Parse(Console.ReadLine());

            if (!Bankomat.TryCreate(id, minLimit, maxLimit, out Bankomat? bankomat))
            {
                Console.WriteLine("Не удалось создать банкомат.");
                Console.ReadKey();
                return;
            }

            string userInput;
            bool isRunning = true;

            while (isRunning)
            {
                Console.Clear();
                Console.WriteLine($"Банкомат: {bankomat}");
                Console.WriteLine($"{CommandLoadCash}) Загрузить купюры" +
                  $"\n{CommandWithdraw}) Снять деньги" +
                  $"\n{CommandBalance}) Показать баланс" +
                  $"\n{CommandInfo}) Информация о банкомате" +
                  $"\n{CommandExit}) Выход");

                Console.Write("Введите команду: ");
                userInput = Console.ReadLine();

                switch (userInput)
                {
                    case CommandLoadCash:
                        Console.Write("Введите номинал (10, 50, 100, 500, 1000): ");
                        if (int.TryParse(Console.ReadLine(), out int nominal) &&
                            Enum.IsDefined(typeof(Nominal), nominal))
                        {
                            Console.Write("Количество купюр: ");
                            if (int.TryParse(Console.ReadLine(), out int count) && count > 0)
                            {
                                bankomat.LoadCash(nominal, count);
                                Console.WriteLine($"Загружено {count} x {nominal} руб.");
                            }
                            else
                            {
                                Console.WriteLine("Некорректное количество.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Некорректный номинал.");
                        }
                        break;

                    case CommandWithdraw:
                        Console.Write("Введите сумму для снятия: ");
                        if (int.TryParse(Console.ReadLine(), out int amount))
                        {
                            if (bankomat.TryWithdraw(amount))
                                Console.WriteLine($"Успешно снято: {amount} руб.");
                            else
                                Console.WriteLine("Снятие невозможно (недостаточно средств, нарушен лимит или сумма не кратна номиналам).");
                        }
                        else
                        {
                            Console.WriteLine("Некорректный ввод.");
                        }
                        break;

                    case CommandBalance:
                        Console.WriteLine($"Текущий баланс: {bankomat.TakeBalance()} руб.");
                        break;

                    case CommandInfo:
                        Console.WriteLine(bankomat);
                        break;

                    case CommandExit:
                        isRunning = false;
                        break;

                    default:
                        Console.WriteLine("Неизвестная команда.");
                        break;
                }

                if (isRunning)
                {
                    Console.WriteLine("\nНажмите любую клавишу...");
                    Console.ReadKey();
                }
            }
        }
    }
}
