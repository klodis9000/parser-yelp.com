IZennoList proxy = project.Lists["proxy"];//������������ � ������ � ������
IZennoList keys = project.Lists["keys"];//������������ � ������ � �������

string pathDir = project.Directory;//���������� ��� ���� � �������
string pathFileOut = pathDir.TrimEnd('\\') + @"\Out.csv";//���� � ��������� �����

string near = String.Empty;//���������� ��� ����� near (��� ���� - �����)
string find = String.Empty;//���������� ��� ����� find (��� ����)

string url = String.Empty;//���������� ��� url
string response = String.Empty;//���������� ��� ������

int pageCurrDef = 20;//���������� ���������� ������� �� ��������

if (proxy.Count == 0)//�������� ������ � ������
	throw new Exception("������ ������ ����!");

do//���� ��� �������� �� ������
{
	int pageCount = 0;//������� ������� ��������
	
	lock(SyncObjects.ListSyncer)//���������� ������ ��� ��������� ������
	{
		if (keys.Count == 0)//�������� ������������� ������ � �������
			throw new Exception("������ � ������� ����!");
		
		string dataKeys = keys[0];//��������� ������ (0) ������ �� ������ � �������
		keys.RemoveAt(0);
		
		string[] nearFind = dataKeys.Split('|', ';', ':');//������ � ���������� �������������
		
		find = nearFind[0];//������������ ���������� find ��������
		near = nearFind[1];//������������ ���������� near ��������
		
		url = String.Format("https://www.yelp.com/search?find_desc={0}&find_loc={1}&ns=1", find, near);//������������ url
		project.SendInfoToLog(url);//����� � ��� url
		string proxys = proxy[0];//��������� ������ �� ������
		
		response = ZennoPoster.HttpGet(url, proxys, "UTF-8", ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.HeaderAndBody, 15000, "");//, project.Profile.UserAgent).HtmlDecode();// ������
		
		HtmlDocument docDef = new HtmlDocument();//����� ������ ������ HtmlDocument
		docDef.LoadHtml(response);//�������� ������ � ������ docDef
		
		HtmlNode elNumber = docDef.DocumentNode.SelectSingleNode("//div[contains(@class, 'pagination-links')]//a[contains (@class, 'next-link')]");//������� ������ �� ��������� ��������
		
		//������� ����� - ������� ������� �� ��������
		if (elNumber != null)
		{
			string nodeDefault = elNumber.Attributes["href"].Value;
			project.SendInfoToLog(nodeDefault);
			//return nodeDefault;
			nodeDefault = Regex.Match(nodeDefault, "(?<=start=).*").Value;
			
			pageCurrDef = Convert.ToInt32(nodeDefault);
			project.SendInfoToLog(pageCurrDef.ToString());
		}		
	}
	
	do//���� ��� �������� �� ���������
	{
		int pageCurr = 0;//������� ��������
		
		HtmlDocument doc = new HtmlDocument();//����� ������ ������ HtmlDocument
		
		lock(SyncObjects.ListSyncer)//���������� ��� �����������. �������� �������� ��� ��������
		{
			//�������� ���� �� � url ������ �������
			if(!url.Contains("start"))
				url += "&start=0";
			
			//�������� ������������� �����
			if(!File.Exists(pathFileOut)) 
				File.AppendAllText(pathFileOut, "Number;NameCompany;LinkYelp" + Environment.NewLine, Encoding.UTF8);
			
			//��������� ������ ��� �������� ��������� �������
			pageCurr = pageCount;			
			pageCount += pageCurrDef;
		}
		
		//���� �������� �� ������ ������������� url �� ��������� �������� 
		if(pageCurr > 0)
		{
			url = Regex.Match(url, ".*&start=").Value;
			url += pageCurr.ToString();
			
		}
		
		project.SendInfoToLog("�������� ������: " + url);
		
		//���� ��� ������
		bool flagProxy = true;
		
		//�������� ������ �� ���������� �� ������� �������
		for(int i = 0; i < proxy.Count; i++)
		{
			//��������� ������ � ������
			string proxys = proxy[i];
			
			//������
			response = ZennoPoster.HttpGet(url, proxys, "utf-8", ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.HeaderAndBody, 15000, "", project.Profile.UserAgent);
			
			//��������� �� ������
			if(!response.Contains("Sorry, you�re not allowed to access this page."))
			{
				flagProxy = false;//������� ����� � ��������� fale
				break;//���������� ����
			}
			project.SendWarningToLog("������ " + proxy + " � �����. ����� ���������.", true);
		}
		
		//������ �����, ���� �� ��������� � false ������ ��� ������ � ������ � �����
		if(flagProxy)
		{
			throw new Exception("��� ������ � �����. ���������� ������!");			
		}
		
		//�������� ������ � ������ doc
		doc.LoadHtml(response);
		
		//���������� ��������� �����
		HtmlNodeCollection collElements = doc.DocumentNode.SelectNodes("//h3[contains (@class, 'heading')]//a");
		
		//�������� ���� �� � �������� ��������
		if (collElements.Count == 0 )
		{
			project.SendWarningToLog(String.Format("�� ������� �������� �� ��������, ���������� ������! �������� ����������� ������ ���� ������� ������������ ������"), true);
			break;
		}
			
		project.SendInfoToLog(String.Format("���������� ��������� �� �������� - {0}", collElements.Count), true);
		
		//�������� ������ ��� ������
		List<string> lstData = new List<string>();
		
		// ������� ��������� ���������
		foreach (HtmlNode elElements in collElements)
		{
			//��������� ������ �� ��������
			string linkCompany = elElements.Attributes["href"].Value;

			//�������� ���� �� � ���������� ������ ����
			if(!linkCompany.Contains("www.yelp.com"))
			{
				linkCompany = "https://www.yelp.com" + linkCompany;
			}
			
			//�������� ��������� �� ����������, ���� ��, �� �� �������� � ���
			if(linkCompany.Contains("adredir"))
			{
				continue;
			}
									
			project.SendInfoToLog(String.Format("�������� ������ ��� �������� �������� - {0}", linkCompany),true);	
			
			//��������� ����� ��������
			string nameCompany = elElements.InnerText.Replace(";", "").HtmlDecode();
			project.SendInfoToLog(String.Format("�������� ��� �������� - {0}", nameCompany),true);
			
			//��������� ������ �������� � ��������
			string numberCompany = elElements.ParentNode.InnerText;
			string checkNumberCompany = numberCompany.Substring(0,3);//999
			
			//��������� �� ����������
			if(checkNumberCompany.Contains("Ad"))
			{
				numberCompany = "Ad";
			}
			else
			{
				numberCompany = numberCompany.Split('.')[0].Trim();	
			}
			
			project.SendInfoToLog(String.Format("�������� ����� �������� - {0}", numberCompany),true);		

			//���������� ������ � ������
			lstData.Add(yelpPars.Extension.FormLine(numberCompany, nameCompany, linkCompany));//	numberCompany;nameCompany;linkCompany	
			
		}
		
		//���������� ������ ��� �������� ������
		lock(SyncObjects.ListSyncer)
		{
			//���������� ���������� � ����
			File.AppendAllLines(pathFileOut, lstData, Encoding.UTF8);
		}
	}
	while(true);	
}
while(true);