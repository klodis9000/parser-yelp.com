IZennoList proxy = project.Lists["proxy"];//привязывание к списку с прокси
IZennoList keys = project.Lists["keys"];//привязывание к списку с ключами

string pathDir = project.Directory;//переменная для пути к проекту
string pathFileOut = pathDir.TrimEnd('\\') + @"\Out.csv";//путь к выходному файлу

string near = String.Empty;//переменная для ключа near (где ищем - город)
string find = String.Empty;//переменная для ключа find (что ищем)

string url = String.Empty;//переменная для url
string response = String.Empty;//переменная для ответа

int pageCurrDef = 20;//переменная количества записей на странице

if (proxy.Count == 0)//проверка список с прокси
	throw new Exception("Список прокси пуст!");

do//цикл для парсинга по ключам
{
	int pageCount = 0;//счетчик текущей страницы
	
	lock(SyncObjects.ListSyncer)//блокировка списка для получения ключей
	{
		if (keys.Count == 0)//проверка заполненности списка с ключами
			throw new Exception("Список с ключами пуст!");
		
		string dataKeys = keys[0];//получение первой (0) строки из списка с ключами
		keys.RemoveAt(0);
		
		string[] nearFind = dataKeys.Split('|', ';', ':');//массив с указанными разделителями
		
		find = nearFind[0];//присваивание переменной find значение
		near = nearFind[1];//присваивание переменной near значение
		
		url = String.Format("https://www.yelp.com/search?find_desc={0}&find_loc={1}&ns=1", find, near);//формирование url
		project.SendInfoToLog(url);//вывод в лог url
		string proxys = proxy[0];//получение прокси из списка
		
		response = ZennoPoster.HttpGet(url, proxys, "UTF-8", ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.HeaderAndBody, 15000, "");//, project.Profile.UserAgent).HtmlDecode();// запрос
		
		HtmlDocument docDef = new HtmlDocument();//новый объект класса HtmlDocument
		docDef.LoadHtml(response);//загрузка ответа в объект docDef
		
		HtmlNode elNumber = docDef.DocumentNode.SelectSingleNode("//div[contains(@class, 'pagination-links')]//a[contains (@class, 'next-link')]");//парсинг ссылки на следующую страницу
		
		//парсинг числа - сколько записей на странице
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
	
	do//цикл для парсинга по страницам
	{
		int pageCurr = 0;//текущая страница
		
		HtmlDocument doc = new HtmlDocument();//новый объект класса HtmlDocument
		
		lock(SyncObjects.ListSyncer)//блокировка для многопотока. Получаем страницу для парсинга
		{
			//проверка есть ли в url номера записей
			if(!url.Contains("start"))
				url += "&start=0";
			
			//проверка существование файла
			if(!File.Exists(pathFileOut)) 
				File.AppendAllText(pathFileOut, "Number;NameCompany;LinkYelp" + Environment.NewLine, Encoding.UTF8);
			
			//получение номера для парсинга следующих записей
			pageCurr = pageCount;			
			pageCount += pageCurrDef;
		}
		
		//если страница не первая фиормирование url на следующую страницу 
		if(pageCurr > 0)
		{
			url = Regex.Match(url, ".*&start=").Value;
			url += pageCurr.ToString();
			
		}
		
		project.SendInfoToLog("Получили ссылку: " + url);
		
		//флаг для прокси
		bool flagProxy = true;
		
		//проверка прокси на блокировку со стороны сервиса
		for(int i = 0; i < proxy.Count; i++)
		{
			//получение строку с прокси
			string proxys = proxy[i];
			
			//запрос
			response = ZennoPoster.HttpGet(url, proxys, "utf-8", ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.HeaderAndBody, 15000, "", project.Profile.UserAgent);
			
			//заблочена ли прокси
			if(!response.Contains("Sorry, you’re not allowed to access this page."))
			{
				flagProxy = false;//перевод флага в состояние fale
				break;//прерывание цикл
			}
			project.SendWarningToLog("Прокси " + proxy + " в блоке. Берем следующую.", true);
		}
		
		//сверка флага, если не переведен в false значит все прокси в списке в блоке
		if(flagProxy)
		{
			throw new Exception("Все прокси в блоке. Прекращаем работу!");			
		}
		
		//загрузка ответа в объект doc
		doc.LoadHtml(response);
		
		//объявление коллекции нодов
		HtmlNodeCollection collElements = doc.DocumentNode.SelectNodes("//h3[contains (@class, 'heading')]//a");
		
		//проверка есть ли в коллеции элементы
		if (collElements.Count == 0 )
		{
			project.SendWarningToLog(String.Format("Не найдены элементы на странице, прекращаем работу! Возможно закончилась выдача либо введены некорректные данные"), true);
			break;
		}
			
		project.SendInfoToLog(String.Format("Количество элементов на странице - {0}", collElements.Count), true);
		
		//создание список для данных
		List<string> lstData = new List<string>();
		
		// Перебор коллекции элементов
		foreach (HtmlNode elElements in collElements)
		{
			//получение ссылки на компанию
			string linkCompany = elElements.Attributes["href"].Value;

			//просмотр есть ли в полученной ссылке хост
			if(!linkCompany.Contains("www.yelp.com"))
			{
				linkCompany = "https://www.yelp.com" + linkCompany;
			}
			
			//проверка рекламное ли объявление, если да, то не работаем с ним
			if(linkCompany.Contains("adredir"))
			{
				continue;
			}
									
			project.SendInfoToLog(String.Format("Получили ссылку для парсинга компании - {0}", linkCompany),true);	
			
			//получение имени компании
			string nameCompany = elElements.InnerText.Replace(";", "").HtmlDecode();
			project.SendInfoToLog(String.Format("Получили имя компании - {0}", nameCompany),true);
			
			//получение номера компании в каталоге
			string numberCompany = elElements.ParentNode.InnerText;
			string checkNumberCompany = numberCompany.Substring(0,3);//999
			
			//рекламное ли объявление
			if(checkNumberCompany.Contains("Ad"))
			{
				numberCompany = "Ad";
			}
			else
			{
				numberCompany = numberCompany.Split('.')[0].Trim();	
			}
			
			project.SendInfoToLog(String.Format("Получили номер компании - {0}", numberCompany),true);		

			//добавление данных в список
			lstData.Add(yelpPars.Extension.FormLine(numberCompany, nameCompany, linkCompany));//	numberCompany;nameCompany;linkCompany	
			
		}
		
		//блокировка списка для внесения данных
		lock(SyncObjects.ListSyncer)
		{
			//добавление результата в файл
			File.AppendAllLines(pathFileOut, lstData, Encoding.UTF8);
		}
	}
	while(true);	
}
while(true);