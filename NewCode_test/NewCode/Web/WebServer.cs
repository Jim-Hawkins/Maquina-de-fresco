using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace NewCode.Web {
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

            //await HandleIncomingConnections();

            // Close the listener
            listener.Close();
        }

        public async void Stop() {
            _runServer = false;
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

                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/shutdown") {
                        Console.WriteLine("Shutdown requested");
                        _runServer = false;
                    }

                    if (req.Url.AbsolutePath == "/setparams") {

                        //Get parameters
                        string url = req.RawUrl;
                        if (!string.IsNullOrWhiteSpace(url)) {

                            //Get text to the right from the interrogation mark
                            string[] urlParts = url.Split('?');
                            if (urlParts?.Length >= 1) {

                                //The parametes are in the array first position
                                string[] parameters = urlParts[1].Split('&');
                                if (parameters?.Length >= 2) {

                                    // Param 5 => to pass
                                    string[] pass_parts = parameters[5].Split('=');
                                    string pass_temp = pass_parts[1];

                                    if (string.Equals(pass, pass_temp)) {

                                        // Param 0 => Temp max
                                        string[] temp_max_parts = parameters[0].Split('=');
                                    //Console.WriteLine(temp_max_parts[1]);
                                    //Data.temp_max = new string[] { temp_max_parts[1] };
                                    Data.temp_max = temp_max_parts[1].Split(",");
                                    //Data.temp_max = new string[] { "14" };

                                    // Param 1 => Temp min
                                    string[] temp_min_parts = parameters[1].Split('=');
                                    //Data.temp_min = new string[] { temp_min_parts[1] };
                                    Data.temp_min = temp_min_parts[1].Split(",");

                                    //Data.temp_min = new string[] { "12" };

                                    // Param 2 => to display_refresh
                                    string[] display_refresh_parts = parameters[2].Split('=');
                                        Data.display_refresh = Int16.Parse(display_refresh_parts[1]);
                                        //Data.display_refresh = 1000;

                                        // Param 3 => to refresh
                                        string[] refresh_parts = parameters[3].Split('=');
                                        Data.refresh = Int16.Parse(refresh_parts[1]);
                                        //Data.refresh = 1000;


                                        // Param 4 => to round_time
                                        string[] round_time_parts = parameters[4].Split('=');
                                        Data.round_time = round_time_parts[1].Split(",");
                                    //Data.round_time = new string[] { "5" };

                                    if (!tempCheck(Data.temp_max, false) || !tempCheck(Data.temp_min, true)) {
                                            message = "El rango de temperatura m&aacute;ximo es entre 30 y 12 grados C.";
                                        }

                                        else {
                                            message = "Los par&aacute;metros se han cambiado satisfactoriamente. Todo preparado.";
                                            ready = true;
                                        }
                                    }
                                    else {
                                        message = "La contrase&ntilde;a es incorrecta.";
                                    }
                                }
                            }
                        }

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

                        message = "Se ha terminado la ronda con " + Data.time_in_range_temp + "s en el rango indicado.";
                    }

                    // Write the response info
                    string disableSubmit = !_runServer ? "disabled" : "";
                    byte[] data = Encoding.UTF8.GetBytes(string.Format(writeHTML(message), pageViews, disableSubmit));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            });
        }


        public static string mostarDatos(string[] data) {
            if (data == null)
                return "";
            
            string datos = string.Empty;

            for (int i = 0; i < data.Length; i++) {
                datos = datos + data[i] + ",";
            }
            datos = datos.Remove(datos.Length - 1);
            return datos;
        }

        public static bool tempCheck(string[] data, bool tipo) {
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

        public static string writeHTML(string message) {
            // If we are already ready, disable all the inputs
            string disabled = "";
            string start = "";
            // Only show save and cooler mode in configuration mode and start round when we are ready
            string save = "<button type=\"button\" onclick='save()'>Guardar</button>";

            if (ready) {
                disabled = "disabled";
            }

            //string temp = "<a href='#' class='btn btn-primary tm-btn-search' onclick='temp()'>Consultar Temperatura</a>";

            if (ready)
            {
                save = "";
                start = "<button type=\"button\" onclick='start()'>Comenzar Ronda</button>";
            }

            if (Data.is_working)
                start = "";

            //Write the HTML page
            string html = "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
                "<meta charset='utf - 8'>" +
                "<meta http - equiv = 'X-UA-Compatible' content = 'IE=edge'>" +
                "<meta name = 'viewport' content = 'width=device-width, initial-scale=1' > " +
                "<title>Netduino Plus 2 Controller</title>" +
                "<link rel='stylesheet' href='https://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700'>" +
                "<link rel = 'stylesheet' href = 'http://127.0.0.1:8887/css/bootstrap.min.css'>" +
                "<link rel = 'stylesheet' href = 'http://127.0.0.1:8887/css/tooplate-style.css' >" +
                "<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.8.0/chart.js'> </script>" +
            "</head>" +
            "<body style=\"background-image: linear-gradient(#006, #777); height: 100vh; color: wheat;\">" +
                "<script> function save(){{" +
                "console.log(\"SAVE!!\");" +
                "var tempMax = document.forms['params']['tempMax'].value;" +
                "var tempMin = document.forms['params']['tempMin'].value;" +
                "var displayRefresh = document.forms['params']['displayRefresh'].value;" +
                "var refresh = document.forms['params']['refresh'].value;" +
                "var time = document.forms['params']['time'].value;" +
                //"var pass = document.forms['params']['pass'].value;" +
                "location.href = 'setparams?tempMax=' + tempMax + '&tempMin=' + tempMin + '&displayRefresh=' + displayRefresh + '&refresh=' + refresh + '&time=' + time + '&pass=pass';" +
                "}} " +
                "function start(){{location.href = 'start'}}" +
                "</script>" +
                "<div class='col-xs-12 ml-auto mr-auto ie-container-width-fix'>" +
                    "<form name='params' method = 'get' class='tm-search-form tm-section-pad-2' style=\"display: flex; justify-content: center; align-items: center; flex-direction: column; \">" +
                        "<div class='form-group tm-form-element tm-form-element-100'>" +
                            "<p>Temperatura Max <b>(&deg;C)</b> <input name='tempMax' type='text' class='form-control' value='" + mostarDatos(Data.temp_max) + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Temperatura Min <b>(&deg;C)</b> <input name='tempMin' type='text' class='form-control' value='" + mostarDatos(Data.temp_min) + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Duraci&oacute;n Ronda <b>(s)</b> <input name='time' type='text' class='form-control' value='" + mostarDatos(Data.round_time) + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div class='form-group tm-form-element tm-form-element-100'>" +
                            "<p>Cadencia Refresco <b>(ms)</b> <input name='displayRefresh' type='number' class='form-control' value='" + Data.display_refresh + "' " + disabled + "></input></p>" +
                        "</div>" +
                        "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Cadencia Interna <b>(ms)</b> <input name='refresh' type='number' class='form-control' value='" + Data.refresh + "' " + disabled + "></input></p>" +
                        "</div>" +
                        //"<div class='form-group tm-form-element tm-form-element-50'>" +
                            //"<p>Contrase&ntilde;a <input name='pass' type='password' class='form-control'> </input></p>" +
                    "</form>" +
                "<div class='form-group tm-form-element tm-form-element-50'>" +
                     save + start +
                    //"</div>" +
                "</div>" +
                     "<p style='text-align:center;font-weight:bold;'>" + message + "</p>" +
                "</div>" +
            "</body>" +
            "</html>";
            return html;
        }

    }
}
