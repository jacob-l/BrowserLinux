using System;
using Gtk;
using WebKit;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Script.Serialization;

public partial class MainWindow: Gtk.Window
{
    private Dictionary<string, Action<NameValueCollection>> handlers;
    private WebView browser;

    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        Build ();

        CreateBrowser ();

        this.ShowAll ();
    }
    
    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        Application.Quit ();
        a.RetVal = true;
    }

    private void CreateBrowser ()
    {
        //Создаем массив обработчиков доступных для вызова из js
        handlers = new Dictionary<string, Action<NameValueCollection>>
        {
            { "callfromjs", nv => CallJs("showMessage", new object[] { nv["msg"] + " Ответ из С#" }) }
        };

        browser = new WebView ();

        browser.NavigationRequested += (sender, args) =>
        {
            var url = new Uri(args.Request.Uri);
            if (url.Scheme != "mp")
            {
                //mp - myprotocol.
                //Обрабатываем вызовы только нашего специального протокола.
                //Переходы по обычным ссылкам работают как и прежде
                return;
            }
            
            var parameters = System.Web.HttpUtility.ParseQueryString(url.Query);

            handlers[url.Host.ToLower()](parameters);

            //Отменяем переход по ссылке
            browser.StopLoading();
        };

        browser.LoadHtmlString (@"
                <html>
                    <head></head>
                    <body id=body>
                        <h1>Интерфейс</h1>
                        <button id=btn>Вызвать C#</button>
                        <p id=msg></p>

                        <script>
                            function buttonClick() {
                                window.location.href = 'mp://callFromJs?msg=Сообщение из js.';
                            }
                            function showMessage(msg) {
                                document.getElementById('msg').innerHTML = msg;
                            }

                            document.getElementById('btn').onclick = buttonClick;
                        </script>
                    </body>
                </html>
            ", null);

        this.Add (browser);
    }

    public void CallJs(string function, object[] args)
    {
        var js = string.Format(@"
            {0}.apply(window, {1});
        ", function, new JavaScriptSerializer().Serialize(args));

        Gtk.Application.Invoke(delegate {
            browser.ExecuteScript(js);
        });
    }
}
