namespace Hot.HotAPIExtensions {
    /// <summary>
    /// Extensões para Microsoft.AspNetCore.Http.HttpContext
    /// </summary>
    public static class HttpContextExtensions {
        /// <summary>
        /// Devolve o IP de origem da requisição, considerendo o X-Forwarded-For quando existir.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string IP_Origem(this HttpContext p) {
            String ip = p.Request.Headers["X-Forwarded-For"].First()
                ?? p.Connection.RemoteIpAddress?.ToString()
                ?? p.GetServerVariable("REMOTE_ADDR")
                ?? "";
//                ip = ip.Split(',')[0];
            return ip;
           
        }
    }
}
