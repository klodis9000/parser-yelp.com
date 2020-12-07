//������ ��� ������ ���������� �� �����
List<string> file = null;

int countBlockLine = 0;
//������� ���������� �����
int lineNumber = 1;

//������� ��� �������� ������
Dictionary <string, string> dictLineCurr = new Dictionary<string, string>();

//������ ��� ����������
List<string> header = null;

IZennoList proxy = project.Lists["proxy"];

if (proxy.Count == 0)
	throw new Exception("������ ������ ����!");

//���� � ���������� �������
string pathDir = project.Directory;
//���� � ���������� � ������
string pathFileOut = pathDir.TrimEnd('\\') + @"\Out.csv";


do
{
	string response = String.Empty;//���������� ��� ������
	
	string lineCurr = String.Empty;//������� ������
	
	int lineNumberCurr = 0;//����� ������� ������
		
	lock(SyncObjects.ListSyncer)
	{
		//�������� ������ ���� � ������� � ������
		if(file == null)
		{
			file = File.ReadAllLines(pathFileOut).ToList();	
			header = file[0].Split(';').ToList();			
		}
		
		//������������ ������� ���������� ����� � ����������� ����� � �����, ���� ������ ��� �����, ������ ��������� ��� ������
		if(lineNumber >= file.Count)
		{
			project.SendInfoToLog("������� ���������� ��� ������ � �����", true);
			break;
		}
		
		//����������� �������� ������� ������ �������� �������� ������� ������ � �����
		lineNumberCurr = lineNumber;
		//������������ �������� ������� ������ �� 1
		lineNumber++;
		
		//��������� ������� ������
		lineCurr = file[lineNumberCurr];
		
		project.SendInfoToLog(String.Format("�������� ������ ��� �������� - {0}", lineCurr), true);	
		
		//�������� ����������� � �������
		string[] checkLineCurr = lineCurr.Split(';');
		
		//�������� ���������� ��������� � �������, ���� ������ 3 ������ ��� ������ ��� ����������
		if (checkLineCurr.Length > 3)
		{
			project.SendWarningToLog(String.Format("������ {0} ��� ����������, ����� ���������.", lineCurr),true);
			continue;
		}		
	}
	
	//�������� ����������� � �������
	string[] arrLineCurr = lineCurr.Split(';');
	
	dictLineCurr = new Dictionary <string, string>();
	
	//���������� ������� �������� ����������, ������� ���������
	for (int i = 0; i < header.Count; i++)
	{
		// �������� ����� ��������� ������� (��������� �������� � ������) � ������� �������
		if (arrLineCurr.Count() > i)
		{
			//� ������ ���� ��������
			dictLineCurr.Add(header[i], arrLineCurr[i]);
			project.SendInfoToLog(String.Format("{0} - {1}", header[i], arrLineCurr[i].ToString()));
		}
		else
		{
			//� ������� ��� ��������
			dictLineCurr.Add(header[i], "");
		}		
	}
	
	//��������� ��������� LinkYelp �� �������
	string url = dictLineCurr["LinkYelp"];
	
	//���� ��� ������
	bool flagProxy = true;
	
	//�������� ������
	for(int i = 0; i < proxy.Count; i++)
	{
		//��������� ������
		string proxys = proxy[i];
		//������
		response = ZennoPoster.HttpGet(url, proxys, "utf-8", ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.HeaderAndBody, 25000, "");//, project.Profile.UserAgent, true, 5).HtmlDecode();
		
		//�������� ��������� �� ������
		if(!response.Contains("Sorry, you�re not allowed to access this page."))
		{
			flagProxy = false;//������� ����� � ��������� fale
			break;//���������� ����
		}
		
		project.SendWarningToLog("������ " + proxys + " � �����. ����� ���������.", true);
	}
	
	//������ �����, ���� �� ��������� � false ������ ��� ������ � ������ � �����
	if(flagProxy)
	{
		throw new Exception("��� ������ � �����. ���������� ������!");
	}
	
	//����� ������ ������ HtmlDocument
	HtmlDocument doc = new HtmlDocument();
	
	//�������� ������ � ������ doc
	doc.LoadHtml(response);
	
	//���������� ���� � ������ ��������
	HtmlNode elName = doc.DocumentNode.SelectSingleNode("//h1");
	
	//���� ����  ����� null ����� ���������
	if (elName == null)
	{
		project.SendInfoToLog("�� ������� ��� ��������, ��������� ������!");
		continue;
	}
	
	//���� ���� ��������
	HtmlNode elSite = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'website')]/a");
	string webSite = "No";
	if(elSite != null)
		webSite = elSite.InnerText.Trim();
	
	//���������� ���� � �������
	if(!dictLineCurr.ContainsKey("WebSite"))//�������� ���� �� ��������� ���� � �������
		dictLineCurr.Add("WebSite", webSite);//���� ��� �������� � ���� � ��������
	else
		dictLineCurr["WebSite"] = webSite;//���� ���� ������������ �������� ���������� �����
	
	project.SendInfoToLog(String.Format("�������� ���-���� �������� - {0}", webSite),true);	
	
	//���� ����� �������� ��������
	HtmlNode elTel = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'biz-phone')]");
	string tel = "No";
	if(elTel != null)
		tel = elTel.InnerText.Trim();

	project.SendInfoToLog(String.Format("�������� ������� �������� - {0}", tel),true);	
	
	//���������� ������� � �������
	if (!dictLineCurr.ContainsKey("Tel"))//�������� ���� �� ��������� ���� � �������
		dictLineCurr.Add("Tel", tel);//���� ��� �������� � ���� � ��������		
	else		
		dictLineCurr["Tel"] = tel;//���� ���� ������������� �������� ���������� �����
	
	//���� ���������� �������
	HtmlNode elRaiting = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'biz-rating biz-rating-very-large')]/span[contains(@class, 'review-count rating-qualifier')]");
	string raiting = "No";
	
	if(elRaiting != null)
		raiting = elRaiting.InnerText.Replace("reviews", "").Trim();
	
	//���������� ���������� ������� � �������
	if (!dictLineCurr.ContainsKey("Reviews"))//�������� ���� �� ��������� ���� � �������
		dictLineCurr.Add("Reviews", raiting);//���� ��� �������� � ���� � ��������		
	else		
		dictLineCurr["Reviews"] = raiting;//���� ���� ���������� �������� ���������� �����
	
	//���� �������
	HtmlNode elStars = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'biz-page')]//div[contains(@class, 'i-stars')]");
	string stars = "No";
	
	if(elStars != null)
	{
		stars = elStars.InnerHtml;
		stars = Regex.Match(stars, @"(?<=alt="").*(?=\ star)").Value;
	}
	
	//���������� �������� � �������
	if (!dictLineCurr.ContainsKey("Raiting"))//�������� ���� �� ��������� ���� � �������
		dictLineCurr.Add("Raiting", stars);//���� ��� ���������� � ����� � ��������		
	else		
		dictLineCurr["Raiting"] = stars;//���� ���� ����������� �������� ���������� �����
	
	//���������� ��� ������������ �������� ������
	string LineOut = "";
	
	//���������� ��� ����������� �������� ������
	lock(SyncObjects.ListSyncer)
	{
		//������� ������ ����������, ��� ��������� ����� ��������� � ������� � ���������� �� �������� � ����������� ������
		for (int i = 0; i< header.Count; i++)
		{
			//���� ������� �������� ������� �� ������� - ���������� �������� � �������� ������
			if (dictLineCurr.ContainsKey(header[i]))
			{
				project.SendInfoToLog(header[i]);
				LineOut += dictLineCurr[header[i]] + ";";	
				project.SendInfoToLog(LineOut);
			}
			
		}
		
		//������� ������� �� �� ��������� ����� ��������� � ������� � ���������� �� �������� � ����������� ������
		for (int i = 0; i < dictLineCurr.Count; i++)
		{
			//���� ������ ���������� �� �������� ���������, ������� ���� � ������� - ����������� ����� ��������� � ����� �������� ������
			if (!header.Contains(dictLineCurr.ElementAt(i).Key))
			{
				project.SendInfoToLog(dictLineCurr.ElementAt(i).Key);
				header.Add(dictLineCurr.ElementAt(i).Key);
				LineOut +=dictLineCurr.ElementAt(i).Value + ";";
				project.SendInfoToLog(LineOut);
			}
			
		}
		
		//���������� ������ ������������� ������
		countBlockLine++;
		//���������� ������ ����������� � ����� �� ������ ����������
		file[0] = String.Join(";", header);
		file[lineNumberCurr] = LineOut;
	}
	
	//���������� ����������� � ����
	if(countBlockLine > 0)
	{
		lock(SyncObjects.ListSyncer)
		{
			File.WriteAllLines(pathFileOut, file, Encoding.UTF8);
			countBlockLine = 0;
		}
	}	
}
while(true);


