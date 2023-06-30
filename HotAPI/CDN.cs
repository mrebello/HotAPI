namespace Hot;
/// <summary>
/// Class to include CDN with js or css in the application
/// </summary>
public class CDN {
    public CDN(string url, string fallback) {
        this.url = url;
        this.local_fallback = fallback;
    }

    bool? IsOnLine = null;  // Used to test if CDN is online
    string _url = "";

    /// <summary>
    /// Set url to CDN, and local_fallback to the same stream
    /// </summary>
    public string url {
        get {
            return (IsOnLine ??= checkUrl(_url).Result) ? _url : local_fallback ?? "";
        }
        set {
            _url = value;
        }
    }
    public string? local_fallback { get; set; }
}
