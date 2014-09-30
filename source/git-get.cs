using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// gmcs git-get.cs
// alias git-get='mono /usr/bin/git-get.exe'

namespace Git {
	public class Get {
		protected string username;
		protected string netException;

		public string Repos {
			get {
				return "https://github.com/" + username + "?tab=repositories";
			}
		}
		public string Stars {
			get {
				return "https://github.com/stars/" + username;
			}
		}

		public string Gists {
			get {
				return "https://gist.github.com/" + username;
			}
		}

		private bool FileIsValid(string url) {
			try {
				HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
				request.Method = "HEAD";
				HttpWebResponse response = request.GetResponse() as HttpWebResponse;
				return (response.StatusCode == HttpStatusCode.OK);
			}
			catch (System.Net.WebException we) {
				//return (we.ToString().Contains("The remote server returned an error: (404) Not Found."));
				//Console.WriteLine("Invalid page: " + url);
				netException = we.ToString();
				return false;
			}
			catch (System.Exception e) {
				Console.WriteLine("Exception validating page: " + url);
				Console.WriteLine(e.ToString());
				return false;
			}
		}

		protected bool PageIsValid(string url) {
			try {
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
					using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
						string html =  reader.ReadToEnd();
						bool starError = html.Contains("have any starred repositories yet.");

						return !starError;
					}
				}
			}
			catch (System.Net.WebException we) {
				//return (we.ToString().Contains("The remote server returned an error: (404) Not Found."));
				//Console.WriteLine("Invalid page: " + url);
				netException = we.ToString();
				return false;
			}
			catch (System.Exception e) {
				Console.WriteLine("Exception validating page: " + url);
				Console.WriteLine(e.ToString());
				return false;
			}
		}

		protected bool IsValidRepoLink(string link) {
			if (Regex.Matches(link,  "/" ).Count != 2) {
				return false;
			}
			if (link.Contains("http://")) {
				return false;
			}
			if (link.Contains("/site/")) {
				return false;
			}
			if (link.Contains("/followers")) {
				return false;
			}
			return true;
		}

		protected bool IsValidGistLink(string link) {
			if (IsValidRepoLink(link)) {
				return link.Contains(username);
			}
			return false;
		}

		protected List<string> GetRepoLinksOnPage(string url) {
			string pageHtml = GetPageHTML(url);
			List<string> links = new List<string>();
			links.AddRange(ExtractHyperlinks(pageHtml));
			for (int i = links.Count - 1; i >= 0; --i) {
				if (!IsValidRepoLink(links[i])) {
					links.RemoveAt(i);
					continue;
				}
#if DEBUG
				else {
					Console.WriteLine ("Repo link: " + links [i]);
				}
#endif
			}
			return links;
		}

		protected string[] ExtractHyperlinks(string html) {
			Regex linkParser = new Regex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			MatchCollection matches = linkParser.Matches(html);
			string[] results = new string[matches.Count];
			for (int i = 0, size = matches.Count; i < size; ++i)
				results[i] = matches[i].Groups["url"].Value;
			return results;
		}

		protected string ExtractOgTitle(string html) {
			try {
				Regex linkParser = new Regex(@"<meta.*?property=[""']og:title[""'].*?content=[""'](?<title>.*?)[""'].*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
				MatchCollection matches = linkParser.Matches(html);
				return matches[0].Groups["title"].Value;
			}
			catch (System.Exception e) {
				Console.WriteLine("Could not retrieve original title.");
				Console.WriteLine(e.ToString());
			}
			return null;
		}

		protected string GetPageHTML(string url) {
			try {
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
					using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
						return reader.ReadToEnd().Replace('\n', ' ');
					}
				}
			}
			catch (System.Exception e) {
				Console.WriteLine("Exception retrieving html for: " + url);
				Console.WriteLine(e.ToString());
				return null;
			}
		}

		protected bool DownloadFile(string fileURL, string fileName, string localPath) {
			if (!FileIsValid(fileURL)) {
				Console.WriteLine("Error downloading file: " + fileURL + "\n\t> " + localPath);
				return false;
			}

			Console.WriteLine("Downloading file: " + fileURL);

			if (System.IO.File.Exists(fileName)) {
				Console.WriteLine("Local file " + fileName + " exists, deleting it.");
				System.IO.File.Delete(fileName);
			}

			try {
				using (WebClient Client = new WebClient ()) {
					Client.DownloadFile(fileURL, fileName);
				}
			}
			catch (System.Net.WebException ne) {
				netException = ne.ToString();
				if (netException.Contains("System.UnauthorizedAccessException")) {
					Console.WriteLine("You do not have permision to save to: " + localPath);
				}
				else {
					Console.WriteLine("Download failed: " + fileURL);
					Console.WriteLine(ne.ToString());
				}
				return false;
			}
			catch (System.Exception e) {
				Console.WriteLine("Download failed: " + fileURL);
				Console.WriteLine(e.ToString());
				return false;
			}
			
			Console.WriteLine(fileURL + " -> " + fileName);
			return true;
		}

		public bool SaveRepo(string repoUrl, string localPath) {
			string fileURL = repoUrl + "/archive/master.zip";
			string fileName = repoUrl.Replace("https://github.com/", "");
			fileName = fileName.Replace("/archive/master.zip", "");
			fileName = localPath + fileName.Replace("/", "-") + ".zip";

			return DownloadFile(fileURL, fileName, localPath);
		}

		public bool SaveGist(string gistUrl, string localPath) {
			string fileURL = gistUrl + "/download";
			string html = GetPageHTML(gistUrl);
			string title = ExtractOgTitle(html);
			if (title == null) {
				return false;
			}
			string fileName = localPath + title.Replace("/", "-") + ".tar.gz";

			if (!System.IO.Directory.Exists(localPath)) {
				Console.WriteLine("Creating directory: " + localPath);
				System.IO.Directory.CreateDirectory(localPath);
			}
			return DownloadFile(fileURL, fileName, localPath);
		}

		public bool SaveRepos(string localPath) {
			List<string> allLinks = new List<string>();
			allLinks.AddRange(GetRepoLinksOnPage(Repos));
			for (int i = allLinks.Count - 1; i >= 0; --i) {
				if (!allLinks[i].Contains(username)) {
					allLinks.RemoveAt(i);
				}
			}

			foreach (string link in allLinks) {
				if (!SaveRepo("https://github.com" + link, localPath)) {
					Console.WriteLine("Error saving repo: https://github.com" + link);
				}
			}

			return true;
		}

		public void SaveStars(string localPath) {
			int page = 0;
			while (PageIsValid(Stars + "?direction=desc&page=" + (page + 1) + "&sort=created"))
				page += 1;

			if (page == 0) {
				Console.WriteLine("Error, no pages found.");
				return;
			}
			else Console.WriteLine(page + " star pages found.");

			List<string> allLinks = new List<string>();
			for (int j = 0; j < page; ++j)
				allLinks.AddRange(GetRepoLinksOnPage(Stars + "?direction=desc&page=" + (j + 1) + "&sort=created"));
			Console.WriteLine("Found " + allLinks.Count + " stars.");

			foreach (string link in allLinks) {
				if (!SaveRepo("https://github.com" + link, localPath)) {
					Console.WriteLine("Error saving star: https://github.com" + link);
				}
			}

			Console.WriteLine("Stars saved.");
		}

		public void SaveGists(string localPath) {
			int page = 0;
			List<string> allLinks = new List<string>();
			while (PageIsValid(Gists + "?page=" + (page + 1))) {
				string pageHtml = GetPageHTML(Gists + "?page=" + (page + 1));
				List<string> links = new List<string>();
				links.AddRange(ExtractHyperlinks(pageHtml));
				for (int i = links.Count - 1; i >= 0; --i) {
					if (!IsValidGistLink(links[i])) {
						links.RemoveAt(i);
						continue;
					}
				}
				if (links.Count == 0)
					break;

				allLinks.AddRange(links);
				page += 1;
			}

			if (page == 0) {
				Console.WriteLine("Error, no pages found.");
				return;
			}
			else Console.WriteLine(page + " gist pages found.");
			Console.WriteLine(allLinks.Count + " gist links found.");

			foreach (string link in allLinks) {
				if (!SaveGist("https://gist.github.com" + link, localPath)) {
					Console.WriteLine("Error saving gist: https://gist.github.com" + link);
				}
			}
		}

		public static void Main(string[] args) {
			bool error = args.Length != 2;

			Get app = new Get();
			string workingDirectory = Directory.GetCurrentDirectory();
			if (workingDirectory[workingDirectory.Length - 1] != '/')
				workingDirectory += "/";
			Console.WriteLine("Working directory: " + workingDirectory);

			string mode = "all";
			if (!error) {
				mode = args [0].ToLower ();
				app.username = args[1];
			}
#if DEBUG
			error = false;
			app.username = "gszauer";
#endif

			if (!error && mode == "all") {
				if (app.PageIsValid(app.Repos)) {
					Console.WriteLine("Retrieving url: " + app.Repos);
					app.SaveRepos(workingDirectory);
				}
				else {
					Console.WriteLine("Invalid repo url: " + app.Repos);
				}

				if (app.PageIsValid(app.Stars)) {
					Console.WriteLine("Retrieving url: " + app.Stars);
					app.SaveStars(workingDirectory);
				}
				else {
					Console.WriteLine("Invalid star url: " + app.Stars);
				}

				if (app.PageIsValid(app.Gists)) {
					Console.WriteLine("Retrieving url: " + app.Gists);
					app.SaveGists(workingDirectory + "Gists/");
				}
				else {
					Console.WriteLine("Invalid gist url: " + app.Gists);
				}
			}
			else if (!error && mode == "star") {
				if (app.PageIsValid(app.Stars)) {
					Console.WriteLine("Retrieving url: " + app.Stars);
					app.SaveStars(workingDirectory);
				}
				else {
					Console.WriteLine("Invalid star url: " + app.Stars);
				}
			}
			else if (!error && mode == "gist") {
				if (app.PageIsValid(app.Gists)) {
					Console.WriteLine("Retrieving url: " + app.Gists);
					app.SaveGists(workingDirectory + "Gists/");
				}
				else {
					Console.WriteLine("Invalid gist url: " + app.Gists);
				}
			}
			else if (!error && mode == "repo") {
				if (app.PageIsValid(app.Repos)) {
					Console.WriteLine("Retrieving url: " + app.Repos);
					app.SaveRepos(workingDirectory);
				}
				else {
					Console.WriteLine("Invalid repo url: " + app.Repos);
				}
			}
			
			if (error) {
				Console.WriteLine("Usage: ");
				Console.WriteLine("\tgit-get all <username>");
				Console.WriteLine("\tgit-get star <username>");
				Console.WriteLine("\tgit-get gist <username>");
				Console.WriteLine("\tgit-get repo <username>");
			}
		}
	}
}