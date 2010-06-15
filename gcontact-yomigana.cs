// Build with: gmcs -pkg:gdata-sharp-contacts gcontact-yomigana.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Google.GData.Contacts;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.Contacts;


public class Driver
{
	const string app_name = "TestAddressImporter";
	const string server_url_param = "http://veritas-vos-liberabit.com/yomigana/yomigana.cgi?source=";

	public static bool RemoteCertificateValidationCallback (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		return true;
	}

	static bool cleanup;

	public static void Main (string [] args)
	{
		if (args.Length < 2) {
			Console.WriteLine ("usage: gcontact-yomigana.exe [username] [password] [--delete]");
			return;
		}
		if (args.Length > 2 && args [2] == "--delete") {
			Console.WriteLine ("Cleanup mode");
			cleanup = true;
		}

		ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
		string username = args [0];
		string password = args [1];
		var rs = new RequestSettings (app_name, username, password);
		rs.AutoPaging = true;
		var cr = new ContactsRequest (rs);
#if false
		var cq = new ContactsQuery (ContactsQuery.CreateContactsUri(null));
		cq.Group = "My Contacts";
		var results = cr.Get<Contact> (cq);
#else
		var results = cr.GetContacts ();
#endif

		if (cleanup) {
			foreach (var c in results.Entries) {
				// these silly null check is required since
				// setting value to nonexistent field causes AE.
				if (c.Name.FamilyNamePhonetics != null)
					c.Name.FamilyNamePhonetics = null;
				if (c.Name.GivenNamePhonetics != null)
					c.Name.GivenNamePhonetics = null;
#if false // this does not work
				if (c.ContactEntry.Dirty)
					Console.WriteLine ("{0} {1} being updated", c.Name.FamilyName, c.Name.GivenName);
#else
				cr.Update<Contact> (c);
#endif
			}
#if false // Probably this does not work for extensions
			results.AtomFeed.Publish ();
#endif
			return;
		}

		var l = new List<string> ();
		var dic = new Dictionary<string,string> ();
		foreach (var c in results.Entries)
			CollectName (c.Name, l);

		// query to mecab server
		string req = String.Join (" ", l.ToArray ());
		byte [] bytes = new WebClient ().DownloadData (server_url_param + req);
		string res = Encoding.UTF8.GetString (bytes);
		string [] rl = res.Split (' ');
		if (rl.Length != l.Count)
			throw new Exception ("Some error occured. I cannot handle address book entry that contains 'm(__)m'.");
		for (int i = 0; i < l.Count; i++) {
			var dst = rl [i].Replace ("m(__)m", " ");
			if (l [i] != dst)
				dic [l [i]] = dst;
		}
		foreach (var p in dic)
			Console.Write ("{0}=> {1}, ", p.Key, p.Value);

		// update
		foreach (var c in results.Entries)
			UpdateName (c, dic, cr);
#if false // Probably this does not work for extension fields.
		results.AtomFeed.Publish ();
#endif
	}
	
	static string Escape (string s)
	{
		return s.Replace (" ", "m(__)m");
	}

	static void CollectName (Name name, List<string> l)
	{
		if (name.FamilyNamePhonetics != null && name.GivenNamePhonetics != null)
			return;
		if (IsYomiTarget (name.FamilyName))
			l.Add (Escape (name.FamilyName));
		if (IsYomiTarget (name.GivenName))
			l.Add (Escape (name.GivenName));
	}

	static void UpdateName (Contact c, Dictionary<string,string> dic, ContactsRequest cr)
	{
		var name = c.Name;
		if (name.FamilyNamePhonetics != null && name.GivenNamePhonetics != null || !IsYomiTarget (name.FamilyName) && !IsYomiTarget (name.GivenName))
			return;
		if (name.FamilyNamePhonetics == null && IsYomiTarget (name.FamilyName) && dic.ContainsKey (name.FamilyName))
			name.FamilyNamePhonetics = dic [name.FamilyName];
		if (name.GivenNamePhonetics == null && IsYomiTarget (name.GivenName) && dic.ContainsKey (name.GivenName))
			name.GivenNamePhonetics = dic [name.GivenName];
		Console.WriteLine ("Setting {0} {1} => {2} {3}", name.FamilyName, name.GivenName, name.FamilyNamePhonetics, name.GivenNamePhonetics);
#if false
#else
		cr.Update<Contact> (c);
#endif
	}

	static bool IsYomiTarget (string s)
	{
		return s != null && s.All (c => c > 0x100);
	}
}

