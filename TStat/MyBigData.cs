using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using System.IO;

using System.Windows;

namespace TStat
{
    public struct Message
    {
        public long id { get; set; }
        public string type { get; set; }
        public DateTime date { get; set; }
        public string from { get; set; }
        public long from_id { get; set; }
        public object text { get; set; }

        public string GetText()
        {
            var elem = (System.Text.Json.JsonElement)text;
            var str = (elem.ValueKind == System.Text.Json.JsonValueKind.String) ? elem.GetString() : null;
            return str;
        }
    }

    public class Chat
    {
        public string name { get; set; }
        public string type { get; set; }
        public long id { get; set; }
        public List<Message> messages { get; set; }
    }

    public class TChats
    {
        public string about { get; set; }
        public List<Chat> list { get; set; }
    }

    public class TData
    {
        public string about { get; set; }
        public TChats chats { get; set; }
    }

    public enum OnlyMy
    {
        Any,
        AnySplit,
        My,
        NotMy,
    }

    public enum StatMode
    {
        SplitAll,
        SumPeople,
        SumWords,
    }

    public class Config
    {
        public bool configChanged;

        public const string Config_Version = "1.1";

        public string Version { get; set; }

        public long MyId { get => myId; set { myId = value; configChanged = true; } }
        long myId;

        public string SearchDir { get; set; } = ".//";

        public int GraphLimit { get; set; } = 40;
        public int TableLimit { get; set; } = 5000;
    }

    public struct StatKey
    {
        public string Key;

        public StatKey(string my, string name, string word, int count)
        {
            Key = null;
            My = my/* == "my"*/;
            Name = name;
            Word = word;
            Count = count;
        }

        public void Init()
        {
            Key = My + Name + Word;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is StatKey st && Key == st.Key;
        }

        public static bool operator==(StatKey st1, StatKey st2)
        {
            return st1.Key == st2.Key;
        }

        public static bool operator!=(StatKey st1, StatKey st2)
        {
            return st1.Key != st2.Key;
        }

        public string My { get; set; }
        public string Name { get; set; }
        public string Word { get; set; }
        public int Count { get; set; }
    }

    public class MyBigData
    {
        TData td;

        delegate bool ChatValidator(Chat chat);

        public void LoadMain(string text)
        {
            td = System.Text.Json.JsonSerializer.Deserialize<TData>(text);
        }

        public void LoadMainInc(string text)
        {
            TData ext = System.Text.Json.JsonSerializer.Deserialize<TData>(text);

            foreach (var item in ext.chats.list)
                AddChat(item);
        }

        public void AddChat(Chat item)
        {
            Chat chat = td.chats.list.Find(x => x.id == item.id);

            if (chat == null)
                td.chats.list.Add(item);
            else
                ApplyChatDiff(chat, item);
        }

        public void ApplyChatDiff(Chat oldChat, Chat newChat)
        {
            bool flag = false;

            foreach (var item in newChat.messages)
            {
                if (!flag && item.date > oldChat.messages.Last().date)
                    flag = true;
                if (flag)
                    oldChat.messages.Add(item);
            }
        }

        public void LoadDialog(string text)
        {
            Chat chat = System.Text.Json.JsonSerializer.Deserialize<Chat>(text);
            td = new TData();
            td.chats = new TChats();
            td.chats.list = new List<Chat>();
            td.chats.list.Add(chat);
        }

        public void LoadDialogInc(string text)
        {
            Chat chat = System.Text.Json.JsonSerializer.Deserialize<Chat>(text);
            AddChat(chat);
        }

        public void OpenChat(string path)
        {
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                string text = sr.ReadToEnd();

                if (text.Remove(15).Contains("about"))
                {
                    if (td == null)
                        LoadMain(text);
                    else
                        LoadMainInc(text);
                }
                else
                {
                    if (td == null)
                        LoadDialog(text);
                    else
                        LoadDialogInc(text);
                }

            }
        }

        public void DeepSearch(DirectoryInfo di)
        {
            FileInfo[] files = di.GetFiles();
            for (int i = 0; i < files.Length; i++)
                if (files[i].Name.EndsWith(".tg.json") /*&& files[i].Name != "config.json"*/)
                    OpenChat(files[i].FullName);

            DirectoryInfo[] directories = di.GetDirectories();
            for (int i = 0; i < directories.Length; i++)
                DeepSearch(directories[i]);
        }

        public MyBigData(Config config)
        {
            this.config = config;

            //try
            {
                DeepSearch(new DirectoryInfo(String.IsNullOrEmpty(config.SearchDir) ? ".\\" : config.SearchDir));
            }
            //catch (Exception ex)
            {
            //    MessageBox.Show("Не удалось открыть папку с данными!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            //    throw ex;
            }

            if (td == null)
            {
                MessageBox.Show("Ни один файл данных не найден. Проверьте путь к файлам в config.json!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception("Ни один файл данных не найден!");
            }
        }

        Config config;

        DateTime startDate;
        DateTime endDate;
        int daysInOnePoint = 7;

        int GetIndexFromDate(DateTime m)
        {
            return (366 * (m.Year - startDate.Year) + (m.DayOfYear - startDate.DayOfYear)) / daysInOnePoint;
        }

        public DateTime GetDateFromIndex(double i)
        {
            return startDate.AddDays(daysInOnePoint * i);
        }

        int GetMessageLength(Message m)
        {
            var elem = (System.Text.Json.JsonElement)m.text;
            var str = (elem.ValueKind == System.Text.Json.JsonValueKind.String) ? elem.GetString() : null;
            var value = str != null ? str.Length : 120;
            return value;
        }

        StringBuilder sb = new StringBuilder();

        public string OnlyLetters(string s, char spaceChar = ' ')
        {
            bool space = false;

            for (int i = 0; i < s.Length; i++)
                if ( (s[i] >= 'А' && s[i] <= 'Я') )
                {
                    sb.Append((char)(s[i] - 'А' + 'а'));
                    space = false;
                }
            else if ((s[i] >= 'а' && s[i] <= 'я') )
                {
                    sb.Append(s[i]);
                    space = false;
                }
            else if ((s[i] >= 'A' && s[i] <= 'Z'))
                {
                    sb.Append((char)(s[i] - 'A' + 'a'));
                    space = false;
                }
            else if ((s[i] >= 'a' && s[i] <= 'z'))
                {
                    sb.Append(s[i]);
                    space = false;
                }
            else
                {
                    if (!space)
                        sb.Append(spaceChar);
                    space = true;
                }
            string r = sb.ToString();
            sb.Clear();
            return r;
        }

        public string FindMyName()
        {
            foreach (var chat in td.chats.list)
                foreach (var message in chat.messages)
                {
                    if (message.from_id == config.MyId)
                            return message.from;
                }

            MessageBox.Show("Не удалось найти имя по id. Возможно, id неверен!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        public string FindMyId(string myName)
        {
            Regex nameRegex = new Regex(myName);

            foreach (var chat in td.chats.list)
                foreach (var message in chat.messages)
                {
                    if (message.from != null)
                    if (nameRegex.IsMatch(message.from))
                    {
                        config.MyId = message.from_id;
                        return message.from;
                    }
                }

            MessageBox.Show("Не удалось найти id по имени. Проверьте введённое имя!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        public Dictionary<StatKey, int[]> RunWordsCounter(string dialogRegexS, int minMessages, OnlyMy my, string wordRegexS, StatMode mode, DateTime startDate, DateTime endDate, int daysInOnePoint/*, int dictionaryLimit = 50*/)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            this.daysInOnePoint = daysInOnePoint;

            Regex dialogRegex = new Regex(dialogRegexS, RegexOptions.IgnoreCase);
            Regex wordRegex = new Regex(wordRegexS, RegexOptions.IgnoreCase);

            DateTime actualStartDate = DateTime.Now;

            foreach (var chat in td.chats.list)
            {
                if (chat.name == null)
                    continue;

                if (!dialogRegex.IsMatch(chat.name))
                    continue;

                if (chat.messages.Count < minMessages)
                    continue;

                if (chat.messages.Count > 0 && chat.messages[0].date < actualStartDate)
                    actualStartDate = chat.messages[0].date;
            }

            if (actualStartDate > startDate)
                this.startDate = actualStartDate;


            int sz = GetIndexFromDate(this.endDate) + 1;

            Dictionary<StatKey, int[]> words = new Dictionary<StatKey, int[]>();

            foreach (var chat in td.chats.list)
            {
                if (chat.name == null)
                    continue;

                if (!dialogRegex.IsMatch(chat.name))
                    continue;

                if (chat.messages.Count < minMessages)
                    continue;

                string chatPrefix = chat.name + "_";

                foreach (var message in chat.messages)
                    if (
                           my == OnlyMy.Any 
                        || my == OnlyMy.AnySplit
                        || my == OnlyMy.My && message.from_id == config.MyId
                        || my == OnlyMy.NotMy && message.from_id != config.MyId
                        )
                    {
                        if (message.date > endDate.AddDays(1))
                            break;

                        var str = message.GetText();
                        if (str != null)
                        {
                            var w = OnlyLetters(str).Split();
                            for (int i = 0; i < w.Length; i++)
                            {
                                if (String.IsNullOrEmpty(w[i]))
                                    continue;

                                if (!wordRegex.IsMatch(w[i]))
                                    continue;

                                StatKey key = new StatKey();
                                switch (mode)
                                {
                                    case StatMode.SplitAll:
                                        {
                                            key.Name = chatPrefix;
                                            key.Word = w[i];
                                            break;
                                        }
                                    case StatMode.SumWords:
                                        {
                                            key.Name = chatPrefix;
                                            break;
                                        }
                                    case StatMode.SumPeople:
                                        {
                                            key.Word = w[i];
                                            break;
                                        }
                                }
                                if (my == OnlyMy.AnySplit)
                                    key.My = (message.from_id == config.MyId ? "my_" : "not_my_");

                                key.Init();

                                //if (words.Count < dictionaryLimit)
                                {
                                    int index = GetIndexFromDate(message.date);
                                    if (index < 0 || index >= sz)
                                        continue;

                                    if (!words.ContainsKey(key))
                                        words.Add(key, new int[sz]);


                                    words[key][index]++;
                                }

                            }
                        }
                    }
            }

            return words;
        }


    }

}
