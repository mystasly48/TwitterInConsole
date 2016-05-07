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

            //deleteTweet(10);
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
            if (command.Contains("add ")) {
                addAccount(extractParam(command, "add "));

            } else if (command.Contains("switch ")) {
                setAccount((extractParam(command, "switch ")));

            } else if (command.Contains("tweet ")) {
                sendTweet(extractParam(command, "tweet "));

            } else if (command.Contains("delete ")) {
                extractParam(command, "delete "); //未実装

            } else if (command.Contains("exit")) {
                return;

            } else if (command.Contains("help")) {
                Console.WriteLine("-----");
                Console.WriteLine("help - ヘルプを表示");
                Console.WriteLine("exit - プログラムを終了");
                Console.WriteLine("cls - コンソールを初期化");
                Console.WriteLine("current - ログイン中のアカウントを表示");
                Console.WriteLine("list - 登録されているアカウントを表示 *未実装*");
                Console.WriteLine("add [名前] - アカウントを登録");
                Console.WriteLine("switch [名前] - アカウントを切り替え");
                Console.WriteLine("tweet [文字列] - 文字列をツイート");
                Console.WriteLine("tweets [数値] - 過去ツイートを[数値]分表示（最大５０件） *未実装*");
                Console.WriteLine("delete [数値] - [数値]前のツイートを削除（最大２００件前） *未実装*");
                Console.WriteLine("remove [名前] - 登録されているアカウント[名前]を削除 *未実装*");
                Console.WriteLine("media [文字列] [場所] - メディア[場所]を添付して[文字列]をツイート *未実装*");
                Console.WriteLine("-----");

            } else if (command.Contains("list")) {
                doList();

            } else if (command.Contains("current")) {
                doCurrent();

            } else if (command.Contains("cls")) {
                Console.Clear();

            } else {
                Console.WriteLine("不明なコマンドです。 help でコマンドの一覧を確認することができます。");

            }
            readCommand();
        }

        // 登録されているアカウントをすべて取得
        static void doList() {
            var accounts = settings.Twitter;
            if (accounts.Count > 0) {
                var result = "";
                for (int i = 0; i < accounts.Count; i++) {
                    result += accounts[i].Name + ", ";
                }
                result = result.Substring(0, result.Length - 2);
                Console.WriteLine("登録されているアカウント: " + result);
            } else {
                Console.WriteLine("登録されているアカウントはありません。");
            }
        }

        // ログイン中のアカウントを取得
        static void doCurrent() {
            if (tokens == null) {
                Console.WriteLine("現在アカウントにログインしていません。");
            } else {
                Console.WriteLine("ログイン中: " + settings.Selected);
            }
        }

        //static void deleteTweet(int previous) {
        //    var tweets = tokens.Statuses.UserTimeline(include_rts: true, exclude_replies: false, contributor_details: false, screen_name: "30msl", count: previous);
        //    //var delete = tokens.Statuses.Destroy(id: 0000000000);

        //    foreach (var tweet in tweets) {
        //        Console.WriteLine(tweet.Text);
        //        Console.WriteLine(tweet.Id);
        //    }
        //}

        // ツイートの送信
        static void sendTweet(string msg) {
            try {
                if (tokens == null) {
                    Console.WriteLine("アカウントの設定がされていません。");
                    return;
                }
                if (msg == "") {
                    Console.WriteLine("ツイートが入力されていません。");
                    return;
                }
                if (msg.Length > 140) {
                    Console.WriteLine("ツイートは１４０文字以内で入力してください。");
                    return;
                }
                tokens.Statuses.Update(status: msg);
                Console.WriteLine("ツイートが投稿されました。");
            } catch (TwitterException) {
                Console.WriteLine("TwitterException が発生しました。");
                throw;
            }
        }

        // y/n で確認を取る
        static bool confirm() {
            while (true) {
                Console.WriteLine("本当に終了しますか？ [y/n]");
                var answer = read();
                if (answer == "y" || answer == "yes") {
                    return true;
                } else if (answer == "n" || answer == "no") {
                    break;
                }
            }
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
