// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Http.Json;
using System.Web;

string auth_url = "https://accounts.spotify.com/authorize";
string callback_url = "http://localhost:8888/callback";
string client_id = "85bfa24f31c2414eba026ef1bea0c575";

Console.WriteLine("Hello, World!");

var client = new HttpClient();
var request = new HttpRequestMessage(HttpMethod.Get, auth_url);



string token;

while (true)
{
    var context = listener.GetContext();


    var req = context.Request;
    var res = context.Response;

    if (req.QueryString["access_token"] == null)
    {
        res.StatusCode = 200;
        res.ContentType = "text/html";

        var content = File.ReadAllBytes("InsertHash.html");

        res.OutputStream.Write(content, 0, content.Count());
        res.Close();
    }
    else
    {
        token = req.QueryString["access_token"]!;

        res.StatusCode = 200;
        res.ContentType = "text/html";

        var content = File.ReadAllBytes("ClosePage.html");

        res.OutputStream.Write(content, 0, content.Count());
        res.Close();
        break;
    }
}

Console.WriteLine($"Token: {token}");

listener.Stop();

var id_req = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");

id_req.Headers.Add("Authorization", $"Bearer {token}");

var id_response = client.Send(id_req);

Console.WriteLine(await id_response.Content.ReadAsStringAsync());

void openUrl(string url)
{
    System.Diagnostics.Process.Start("explorer", $"\"{url}\"");
}

//request.Header

//var response = await request.GetAsync(auth_url).ConfigureAwait(false);

//Console.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
