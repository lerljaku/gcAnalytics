<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main()
{
	var dir = @"D:\Downloads\fb_AllMessages\messages\inbox\Karetnigaychat_SlenQ6cAJA";
	
	var data = new List<Data>();
	
	foreach (var file in Directory.EnumerateFiles(dir).Where(d => d.EndsWith(".json")))
	{
		var content = File.ReadAllText(file);
				
		var dataPart = JsonConvert.DeserializeObject<Data>(content);

		data.Add(dataPart);

		foreach (var participant in dataPart.participants)
		{
			participant.name = Fix(participant.name);
		}

		foreach (var message in dataPart.messages)
		{
			message.content = Fix(message.content);
			message.sender_name = Fix(message.sender_name);

			foreach (var reaction in (message.reactions ?? new Reaction[0]))
			{
				reaction.actor = Fix(reaction.actor);
				reaction.reaction = Fix(reaction.reaction);
			}
		}
	}

	$"participants: {data.SelectMany(d => d.participants).Select(d => d.name).Distinct().Count()}".Dump();

	var messages = data.SelectMany(d => d.messages).Where(d => d != null).ToList();

	$"prvni zprava: {messages.Min(d => d.Timestamp)}".Dump();

	$"zprav celkem: {messages.Count()}".Dump();
	
	foreach (var sender in messages.GroupBy(m => m.sender_name))
	{
		var messageCnt = sender.Count();
	}

	var messageMap = messages.GroupBy(d => d.sender_name).OrderByDescending(d => d.Count()).Take(16).ToDictionary(m => m.Key, m => m.ToList());

	"zpravy per user".Dump();
	messageMap.Select(m => (m.Key, m.Value.Count(), (int)(((double)m.Value.Count() / (double)messages.Count()) * 100))).OrderByDescending(d => d.Item2).Dump();

	"prumerna delka zpravy per user".Dump();
	messageMap.Select(m => (m.Key, (double)m.Value.Where(d => d.content != null).Sum(d => d.content.Length) / (double)m.Value.Count())).OrderByDescending(d => d.Item2).Dump();

	"pocet pismenek per user".Dump();
	messageMap.Select(m => (m.Key, (double)m.Value.Where(d => d.content != null && !d.content.Contains("https")).Sum(d => d.content.Length))).OrderByDescending(d => d.Item2).Dump();

	"nejvic reakci dostal".Dump();
	{
		"Total".Dump();
		messageMap.Select(m => (m.Key, m.Value.Sum(v => v.reactions == null ? 0 : v.reactions.Count()))).OrderByDescending(d => d.Item2).Dump();

		"ReactionPerMessage".Dump();
		messageMap.Select(m => (m.Key, (double)m.Value.Sum(v => v.reactions == null ? 0 : v.reactions.Count()) / (double)m.Value.Count())).Where(d => d.Item2 > 0).OrderByDescending(d => d.Item2).Dump();
	}
	
	"top 5 prispekvu GC".Dump();
	{
		messages.Where(m => m.reactions != null).OrderByDescending(d => d.reactions.Count()).Take(5).Dump();
	}
	
	"nejvic reakci dal".Dump();
	{
		"Total".Dump();
		var r = messages.Where(d => d.reactions != null).SelectMany(d => d.reactions).GroupBy(d => d.actor).OrderByDescending(d => d.Count()).Select(d => (d.Key, d.Count())).Where(d => messageMap.ContainsKey(d.Key)).ToList();
		r.Dump();

		"ReactionPerMessage".Dump();
		r.Select(d => (d.Key, (double)d.Item2 / (double)messages.Count(m => m.sender_name == d.Key))).OrderByDescending(d => d.Item2).Where(d => messageMap.ContainsKey(d.Key)).Dump();
	}

	var splitChars = new[] { ' ', ',', '.', '@', '[', '(', '{', ']', ')', '}', '?', '+', '*', '/', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', '!', ';', };
	"nejbohatsi slovnik (unikatni slova)".Dump();
	{
		"Total".Dump();

		messageMap.Select(m => (m.Key, m.Value.Select(v => v.content).Where(d => d != null && !d.Contains("https")).SelectMany(v => v.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)).Distinct().Count())).OrderByDescending(d => d.Item2).Dump();
	}

	var ignored = new List<string>(){
		
	};

	"top 30 nejpouzivanejsich slov (delsi nez 3 znaky)".Dump();
	{
		messages.Select(m => m.content)
		.Where(d => d != null && !d.Contains("https"))
		.SelectMany(v => v.Split(splitChars, StringSplitOptions.RemoveEmptyEntries))
		.Where(v => v.Length > 3)
		.GroupBy(v => v)
		.Select(v => (v.Key, v.Count()))
		.OrderByDescending(v => v.Item2)
		.Take(30)
		.Dump();
	}

	"top 30 nejpouzivanejsich slov (delsi nez 3 znaky - prd tam stejne nebyl)".Dump();
	{
		var result = new List<(string, List<(string, int)>)>();

		foreach (var pair in messageMap)
		{
			var frequencyAnalysis = pair.Value.Select(m => m.content)
			.Where(d => d != null && !d.Contains("https"))
			.SelectMany(v => v.Split(splitChars, StringSplitOptions.RemoveEmptyEntries))
			.Where(v => v.Length > 3)
			.GroupBy(v => v)
			.Select(v => (v.Key, v.Count()))
			.OrderByDescending(v => v.Item2)
			.Take(10).ToList();
			
			result.Add((pair.Key, frequencyAnalysis));
		}

		result.Dump();
	}

	"nejvic linku".Dump();
	{
		var shares = messageMap.Select(k => (k.Key, k.Value.Count(m => m.share != null)));
		"Total".Dump();
		shares.OrderByDescending(d => d.Item2).Dump();
		"LinkPerMessage".Dump();
		messageMap.Select(k => (k.Key, (double)k.Value.Count(m => m.share != null) / (double)k.Value.Count())).Where(d => d.Item2 > 0).OrderByDescending(k => k.Item2).Dump();
	}

	"nejvic gifu".Dump();
	{
		var gifs = messageMap.Select(k => (k.Key, k.Value.Count(m => m.gifs != null)));
		"Total".Dump();
		gifs.OrderByDescending(d => d.Item2).Dump();
		"GifPerMessage".Dump();
		messageMap.Select(k => (k.Key, (double)k.Value.Count(m => m.gifs != null) / (double)k.Value.Count())).Where(d => d.Item2 > 0).OrderByDescending(k => k.Item2).Dump();
	}

	"nejvic fotek".Dump();
	{
		var gifs = messageMap.Select(k => (k.Key, k.Value.Count(m => m.photos != null)));
		"Total".Dump();
		gifs.OrderByDescending(d => d.Item2).Dump();
		"FotkaPerMessage".Dump();
		messageMap.Select(k => (k.Key, (double)k.Value.Sum(m => m.photos == null ? 0 : m.photos.Count()) / (double)k.Value.Count())).Where(d => d.Item2 > 0).OrderByDescending(k => k.Item2).Dump();
	}


	"nejvic otazek (ztrimovane otazniky)".Dump();
	{	
		var otazniky = messageMap.Select(k => (k.Key, k.Value.Where(m => m.type == "Generic" && m.content != null && !m.content.Contains("http")).Sum(m => m.content.Replace("??", "?").Count(c => c == '?'))));
		"Total".Dump();
		otazniky.OrderByDescending(d => d.Item2).Dump();
		"OtazkyPerMessage".Dump();
		otazniky.Select(k => (k.Key, (double)k.Item2 / (double)messageMap[k.Key].Count())).Where(d => d.Item2 > 0).OrderByDescending(k => k.Item2).Dump();
	}
	// nejvic smajliku
	var emojis = new List<string>(){":]",":)",":(",":D",".)",":-D"};

	$"nejvic smajliku ({string.Join(", ", emojis)})".Dump();
	{
		var otazniky = messageMap.Select(k => (k.Key, k.Value.Where(m => m.type == "Generic" && m.content != null && !m.content.Contains("http")).Sum(m => emojis.Sum(e => m.content.Split(new [] { e }, StringSplitOptions.None).Length - 1))));
		"Total".Dump();
		otazniky.OrderByDescending(d => d.Item2).Dump();
		"smajlikyPerMessage".Dump();
		otazniky.Select(k => (k.Key, (double)k.Item2 / (double)messageMap[k.Key].Count())).Where(d => d.Item2 > 0).OrderByDescending(k => k.Item2).Dump();
	}
	
	"rozdal nejvice prezdivek (X set the nickname for Y to Z)".Dump();
	{
		messageMap.Select(m => (m.Key, m.Value.Count(v => v.content != null && v.content.Contains(" set the nickname for ")))).OrderByDescending(d => d.Item2).Dump();
	}

	"byla mu nejcasteji prezdivka zmenena".Dump();
	{
		var regex = new Regex(@"^(?<who>.+) set the nickname for (?<person>.+) to (?<nickname>.+).$");
		
		messages.Where(m => m.content != null)
		.Select(m => regex.Match(m.content))
		.Where(m => m.Success)
		.Select(m => m.Groups["person"].ToString())
		.GroupBy(m => m)
		.Select(m => (m.Key, m.Count()))
		.OrderByDescending(m => m.Item2)
		.Dump();
	}

	"hodinovy casovy snimek celkovy".Dump();
	{
		var counts = new int[24];

		foreach (var message in messages)
		{
			counts[message.Timestamp.TimeOfDay.Hours]++;
		}
	
		counts.Select((c, i) => ($"{i} - {(i + 1) % 24}", c)).Dump();
	}

	"hodinovy casovy snimek (osobni)".Dump();
	{
		var times = new List<(string sender, int[] counts)>();
		foreach (var personMessages in messageMap)
		{
			var person = personMessages.Key;
			var msgs = personMessages.Value;
			var counts = new int[24];

			foreach (var msg in msgs)
			{
				counts[msg.Timestamp.TimeOfDay.Hours]++;
			}

			times.Add((person, counts));
		}
		times.Select(t => (t.sender, t.counts.Select((c, i) => ($"{i} - {(i + 1) % 24}", c)))).Dump();
	}
}

public class Data
{
	public Participant[] participants { get; set;}
	public Message[] messages{get;set;}
}

public class Participant{
	public string name{get;set;}
}

public class Message
{
	public string sender_name{get;set;}
	public long timestamp_ms{get;set;}
	public DateTime Timestamp => new DateTime(1970, 1, 1).AddMilliseconds(timestamp_ms);
	public string content{get;set;}
	public string type{get;set;}
	public Photo[] photos{get;set;}
	public Share share{get;set;}
	public Reaction[] reactions{get;set;}
	public Gif[] gifs{get;set;}
}

public class Gif{
	public string uri{get;set;}
}

public class Photo{
	public string uri { get; set; }
	public long creation_timestamp { get; set; }
	public DateTime Timestamp => new DateTime(1970, 1, 1).AddMilliseconds(creation_timestamp);
}

public class Share{
	public string link{get;set;}
}

public class Reaction{
	public string reaction{get;set;}
	public string actor{get;set;}
}

public string Fix(string text)
{
	if (text == null)
	 return null;
	try
	{
		Encoding targetEncoding = Encoding.GetEncoding("ISO-8859-1");
		var unescapeText = System.Text.RegularExpressions.Regex.Unescape(text);
		return Encoding.UTF8.GetString(targetEncoding.GetBytes(unescapeText));
	}
	catch (Exception)
	{
		return text;
	}
}