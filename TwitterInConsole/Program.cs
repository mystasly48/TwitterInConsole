using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreTweet;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TwitterInConsole {
    class Program {
        static Settings settings = new Settings();
        static string file = "settings.xml";
        static Tokens tokens;

        static void Main(string[] args) {
            Console.WriteLine("Mystter - 対話式");
            Console.WriteLine("help でコマンドを確認することができます。");

            load();
            initialSet();

            readCommand();

            save();
        }

        // 保存されていた場合に前回のアカウントに設定
        static void initialSet() {
            if (!String.IsNullOrEmpty(settings.Selected)) {
                tokens = getTokens(settings.Selected);
            }
        }

        // 新規アカウントの登録（認証）
        static void addAccount(string name) {
            try {
                if (getTokens(name) == null) {
                    var s = OAuth.Authorize(Keys.ConsumerKey, Keys.ConsumerSecret);
                    Process.Start(s.AuthorizeUri.AbsoluteUri);
                    Console.WriteLine("PIN を入力してください。");
                    Tokens _tokens = s.GetTokens(read());
                    saveTokens(_tokens, name);
                    Console.WriteLine("アカウント " + name + " が登録されました。");
                    setAccount(name);
                } else {
                    Console.WriteLine(name + " は既に登録されています。");
                }
            } catch (TwitterException) {
                Console.WriteLine("TwitterException が発生しました。");
                Console.WriteLine("どうせ PIN の入力を間違えたんだろ。それの判定が分からねぇんだよな。");
            }
        }

        // 現在のアカウントを name に設定
        static void setAccount(string name) {
            if (getTokens(name) != null) {
                tokens = getTokens(name);
                settings.Selected = name;
                save();
                Console.WriteLine("アカウント " + name + " に切り替えました。");
            } else {
                Console.WriteLine(name + " は登録されていません。");
            }
        }

        // tokens を name で xml に保存
        static void saveTokens(Tokens _tokens, string name) {
            var token = _tokens.AccessToken;
            var secret = _tokens.AccessTokenSecret;
            var screen = _tokens.ScreenName;

            var account = new Account();
            account.Name = name;
            account.Token = token;
            account.Secret = secret;
            account.Screen = screen;
            settings.Twitter.Add(account);

            save();
        }

        // name から Token を取得して返す
        static Tokens getTokens(string name) {
            var account = new Account();
            for (int i = 0; i < settings.Twitter.Count; i++) {
                account = settings.Twitter[i];
                if (account.Name == name) {
                    Tokens _tokens = Tokens.Create(Keys.ConsumerKey, Keys.ConsumerSecret, account.Token, account.Secret);
                    return _tokens;
                }
            }
            return null;
        }



        // 設定ファイルへ保存
        static void save() {
            var s = new XmlSerializer(typeof(Settings));
            var w = new StreamWriter(file, false, Encoding.UTF8);
            s.Serialize(w, settings);
            w.Close();
        }

        // 設定ファイルの読み込み
        static void load() {
            if (File.Exists(file)) {
                var s = new XmlSerializer(typeof(Settings));
                var r = new StreamReader(file);
                settings = (Settings)s.Deserialize(r);
                r.Close();
            } else {
                save();
            }
        }

        // コマンドの判定
        static void command(string command) {
            // exit
            // help
            // list
            // deleteLatest 廃止
            // add(name) => add name
            // tweet(message) => tweet message
            // switch(name) => switch name
            // delete(int) => delete int
            if (command.Contains("cls")) {
                Console.Clear();
            } else if (command.Contains("exit")) {
                return;
            } else if (command.Contains("help")) {

            } else if (command.Contains("list")) {

            } else if (command.Contains("delete ")) {
                extractParam(command, "delete ");
            } else if (command.Contains("tweet ")) {
                sendTweet(extractParam(command, "tweet "));
            } else if (command.Contains("switch ")) {
                setAccount((extractParam(command, "switch ")));
            } else if (command.Contains("add ")) {
                addAccount(extractParam(command, "add "));
            } else {
                Console.WriteLine("不明なコマンドです。 help でコマンドの一覧を確認することができます。");
            }

            readCommand();
        }

        static void deleteTweet(int pre) {
            
        }

        static void sendTweet(string msg) {
            try {
                if (tokens == null) {
                    Console.WriteLine("アカウントの設定がされていません。");
                    return;
                }
                tokens.Statuses.Update(status: msg);
                Console.WriteLine("ツイートが投稿されました。");
            } catch (TwitterException) {
                Console.WriteLine("TwitterException が発生しました。");
                throw;
            }
        }

        static bool confirm() {
            //while (true) {
            //    Console.WriteLine("本当に終了しますか？ [y/n]");
            //    var answer = read();
            //    if (answer == "y" || answer == "yes") {
            //        return;
            //    } else if (answer == "n" || answer == "no") {
            //        break;
            //    }
            //}
            return false;
        }

        // コマンドのパラメーターを返す（正規表現）
        static string extractParam(string original, string command) {
            var pattern = "(" + command + ")(?<param>.+?)$";
            var result = Regex.Match(original, pattern).Groups["param"].Value;
            return result;
        }

        //// コマンドのパラメーターを返す（置換）
        //static string extractParam(string original, string command) {
        //    var result = original.Replace(command, "");
        //    return result;
        //}

        // コマンドの入力を待機して返す
        static string read() {
            Console.Write(">> ");
            var input = Console.ReadLine();
            return input;
        }
        
        // コマンドの入力を待機して返されたらコマンドの判定に回す
        static void readCommand() {
            var input = read();
            command(input);
        }
    }

    public class Settings {
        public string Selected;
        public List<Account> Twitter = new List<Account>();
    }

    public class Account {
        public string Name;
        public string Token;
        public string Secret;
        public string Screen;
    }
}
