using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;

namespace NewCode.Web
{
    public class WebServer {

        private IPAddress _ip = null;
        private int _port = -1;
        private bool _runServer = true;
        private static HttpListener listener;
        private static int pageViews = 0;
        private static int requestCount = 0;
        private static bool ready = false;
        private static readonly string pass = "pass";
        private static string message = "";

        private static Regex tempRegex   = new Regex(@"^(tempMax|tempMin)=([0-9][0-9],)*[0-9][0-9]$");
        private static Regex roundTimeRegex = new Regex(@"^time=([0-9]+,)*[0-9]+$");
        private static Regex refreshRegex   = new Regex(@"^(refresh|displayRefresh)=[0-9]+$");

        /// <summary>
        /// Delegate for the CommandReceived event.
        /// </summary>
        public delegate void CommandReceivedHandler(object source, WebCommandEventArgs e);

        /// <summary>
        /// CommandReceived event is triggered when a valid command (plus parameters) is received.
        /// Valid commands are defined in the AllowedCommands property.
        /// </summary>
        public event CommandReceivedHandler CommandReceived;

        public string Url {
            get {
                if (_ip != null && _port != -1) {
                    return $"http://{_ip}:{_port}/";
                }
                else {
                    return $"http://127.0.0.1:{_port}/";
                }
            }
        }

        public WebServer(IPAddress ip, int port) {
            _ip = ip;
            _port = port;
        }

        public async void Start() {
            if (listener == null) {
                listener = new HttpListener();
                listener.Prefixes.Add(Url);
            }
            listener.Start();
            Console.WriteLine($"The url of the webserver is {Url}");

            // Handle requests
            while (_runServer)
            {
                await HandleIncomingConnections();
            }
            // Close the listener
            listener.Close();
        }

        private String[] HandleSetParams(string url)
        {
            String[] res = new String[2];
            res[1] = "f";

            if (!url.Contains("?"))
            {
                res[0] = "Faltan los campos tempMax, tempMin, timeRound, refresh, internalRefresh, pass";
                return res;
            }

            url = url[(url.IndexOf("?") + 1)..];

            if (!url.Contains("&"))
            {
                res[0] = "Faltan campos (separados por &)";
                return res;
            }

            string[] parameters = url.Split('&');

            if (parameters.Length != 6)
            {
                res[0] = "Introduzca los campos tempMax, tempMin, timeRound, refresh, internalRefresh, pass";
                return res;
            }

            for(int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].Contains("="))
                {
                    res[0] = "Alg&uacute;n campo está mal formado";
                    return res;
                }
            }

            if (parameters[5].Split("=")[1] != pass)
            {
                res[0] = "Contrase&ntilde;a incorrecta";
                return res;
            }

            // Param 0 => Temp max
            if (!tempRegex.Match(parameters[0]).Success)
            {
                res[0] = "El formato de temperatura m&aacute;xima es ^tempMax=([0-9][0-9],)*[0-9][0-9]$";
                return res;
            }
            string[] temp_max_parts = parameters[0].Split('=')[1].Split(",");
            if (!TempCheck(temp_max_parts, false))
            {
                res[0] = "La temperatura m&aacute;xima es 30 grados Celsius";
                return res;
            }
            Data.temp_max = parameters[0].Split('=')[1].Split(",");

            // Param 1 => Temp min
            if (!tempRegex.Match(parameters[1]).Success)
            {
                res[0] = "El formato de temperatura m&iacute;nima es ^tempMin=([0-9][0-9],)*[0-9][0-9]$";
                return res;
            }
            string[] temp_min_parts = parameters[1].Split('=')[1].Split(",");
            if (!TempCheck(temp_min_parts, true))
            {
                res[0] = "La temperatura m&iacute;nima es 12 grados Celsius";
                return res;
            }
            Data.temp_min = parameters[1].Split("=")[1].Split(",");

            // Param 2 => to display_refresh
            if (!refreshRegex.Match(parameters[2]).Success || Int16.Parse(parameters[2].Split('=')[1]) <= 0)
            {
                res[0] = "El formato de tiempo de refresco es ^displayRefresh=[0-9]+$ y el valor debe ser mayor que cero";
                return res;
            }
            Data.display_refresh = Int16.Parse(parameters[2].Split('=')[1]);

            // Param 3 => to refresh
            if (!refreshRegex.Match(parameters[3]).Success || Int16.Parse(parameters[3].Split('=')[1]) <= 0)
            {
                res[0] = "El formato de tiempo de refresco es ^refresh=[0-9]+$ y el valor debe ser mayor que cero";
                return res;
            }
            Data.refresh = Int16.Parse(parameters[3].Split('=')[1]);

            // Param 4 => to round_time
            if (!roundTimeRegex.Match(parameters[4]).Success)
            {
                Console.WriteLine("tiempo de ronda: " + parameters[4]);
                res[0] = "El formato de tiempo de ronda es ^time=([0-9]+,)*[0-9]+$";
                return res;
            }
            Data.round_time = parameters[4].Split('=')[1].Split(",");

            if (Data.round_time.Length != Data.temp_max.Length || Data.round_time.Length != Data.temp_min.Length)
            {
                res[0] = "El número de rangos no coincide entre temperaturas máximas, mínimas y tiempos";
                return res;
            }

            res[0] = "Los par&aacute;metros se han cambiado satisfactoriamente. Todo preparado.";
            res[1] = "t";
            return res;
        }

        private async Task HandleIncomingConnections() {

            await Task.Run(async () => {
                // While a user hasn't visited the `shutdown` url, keep on handling requests
                while (_runServer) {

                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync();

                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    // Print out some info about the request
                    Console.WriteLine("Request #: {0}", ++requestCount);
                    Console.WriteLine(req.Url);
                    Console.WriteLine(req.HttpMethod);
                    Console.WriteLine(req.UserHostName);
                    Console.WriteLine(req.UserAgent);
                    Console.WriteLine();

                    if (req.Url.AbsolutePath == "/setparams") {
                        //Get parameters
                        String[] result = HandleSetParams(req.RawUrl);
                        message = result[0];
                        ready = Equals(result[1], "t");
                    }

                    if (req.Url.AbsolutePath == "/start") {
                        Console.WriteLine("empieza ronda");
                        int rango = 0;
                        for (int i = 0; i < Data.round_time.Length; i++)
                        {
                            rango += int.Parse(Data.round_time[i]);
                        }
                        // Start the round
                        Thread ronda = new Thread(MeadowApp.StartRound);
                        ronda.Start();

                        // Wait for the round to finish
                        Thread.Sleep(rango*1000 + 2000);
                        ready = false;
                        message = $"Se ha terminado la ronda con {Data.time_in_range_temp} s en el rango indicado.";
                    }

                    // Write the response info
                    string disableSubmit = _runServer ? "" : "disabled";
                    /************************************************/
                    //byte[] data = Encoding.UTF8.GetBytes(string.Format(WriteHTML(message), pageViews, disableSubmit));
                    byte[] data = Encoding.UTF8.GetBytes(WriteHTML(message));
                    /************************************************/
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            });
        }


        public static string MostarDatos(string[] data) {
            if (data == null)
                return "";
            
            string datos = string.Empty;

            for (int i = 0; i < data.Length; i++) {
                datos = datos + data[i] + ",";
            }
            datos = datos.Remove(datos.Length - 1);
            return datos;
        }

        public static bool TempCheck(string[] data, bool tipo) {
            if (data == null)
                return true;

            for (int i = 0; i < data.Length; i++) {
                if (tipo) {
                    if (Double.Parse(data[i].ToString()) < 12) {
                        return false;
                    }
                }
                else {
                    if (Double.Parse(data[i].ToString()) > 30) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static string WriteHTML(string message) {
            // If we are already ready, disable all the inputs
            string disabled = "";
            string start = "";

            // Only show save and cooler mode in configuration mode and start round when we are ready
            string save = "<button type=\"button\" onclick='save()'>Guardar</button>";

            //string temp = "<a href='#' class='btn btn-primary tm-btn-search' onclick='temp()'>Consultar Temperatura</a>";

            if (ready)
            {
                save = "";
                disabled = "disabled";
                start = "<button type=\"button\" onclick='start()'>Comenzar Ronda</button>";
            }
            if (Data.is_working)
                start = "";

            //Write the HTML page
            return 
            "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
                "<meta charset='utf - 8'>" +
                "<meta http - equiv = 'X-UA-Compatible' content = 'IE=edge'>" +
                "<meta name = 'viewport' content = 'width=device-width, initial-scale=1' > " +
                "<title>Meadow Controller</title>" +
                "<link rel='stylesheet' href='https://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700'>" +
                //"<link rel = 'stylesheet' href = 'http://127.0.0.1:8887/css/bootstrap.min.css'>" +
                //"<link rel = 'stylesheet' href = 'http://127.0.0.1:8887/css/tooplate-style.css' >" +
                //"<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.8.0/chart.js'> </script>" +
            "</head>" +
            "<body style=\"background-image: linear-gradient(#006, #777); height: 100vh; color: wheat;\">" +
                "<script> function save(){{" +
                    "console.log(\"SAVE!!\");" +
                    "var time = document.forms['params']['time'].value;" +
                    "var tempMax = document.forms['params']['tempMax'].value;" +
                    "var tempMin = document.forms['params']['tempMin'].value;" +
                    "var refresh = document.forms['params']['refresh'].value;" +
                    "var displayRefresh = document.forms['params']['displayRefresh'].value;" +
                    "location.href = 'setparams?tempMax=' + tempMax + '&tempMin=' + tempMin + '&displayRefresh=' + displayRefresh + '&refresh=' + refresh + '&time=' + time + '&pass=pass';" +
                    "}} " +
                "function start(){{location.href = 'start'}}" +
                "</script>" +
                "<div>" +
                    "<form name='params' method='get' style=\"display: flex; justify-content: center; align-items: center; flex-direction: column; \">" +
                        "<div>" +
                            "<p>Temperatura Max <b>(&deg;C)</b><input name='tempMax' type='text' value='" + MostarDatos(Data.temp_max) + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div>" +
                            "<p>Temperatura Min <b>(&deg;C)</b><input name='tempMin' type='text' value='" + MostarDatos(Data.temp_min) + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div>" +
                            "<p>Duraci&oacute;n Ronda <b>(s)</b><input name='time' type='text' value='" + MostarDatos(Data.round_time) + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div>" +
                            "<p>Cadencia Refresco <b>(ms)</b><input name='displayRefresh' type='number' value='" + Data.display_refresh + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div>" +
                            "<p>Cadencia Interna <b>(ms)</b><input name='refresh' type='number' value='" + Data.refresh + "' " + disabled + "></input></p>" +
                        "</div>" +
                    "</form>" +
                    "<div style=\"display: flex; justify-content: center; align-items: center; flex-direction: column; \">" +
                        save +
                        start +
                    "</div>" +
                     "<p style='text-align:center;font-weight:bold;'>" + message + "</p>" +
                "</div>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
                "<img src='https://vignette.wikia.nocookie.net/memes-pedia/images/c/cb/Rick-roll-o.gif/revision/latest?cb=20150916225117&path-prefix=es'" +
                "alt='jose pépez'/>" +
            "</body>" +
            "</html>";
        }
    }
}
