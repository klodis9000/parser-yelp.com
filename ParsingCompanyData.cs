//список для данных полученных из файла
List<string> file = null;

int countBlockLine = 0;
//счетчик количества строк
int lineNumber = 1;

//словарь для хранения данных
Dictionary <string, string> dictLineCurr = new Dictionary<string, string>();

//список для заголовков
List<string> header = null;

IZennoList proxy = project.Lists["proxy"];

if (proxy.Count == 0)
	throw new Exception("Список прокси пуст!");

//путь к директории проекта
string pathDir = project.Directory;
//путь к директории с файлом
string pathFileOut = pathDir.TrimEnd('\\') + @"\Out.csv";


do
{
	string response = String.Empty;//переменная для ответа
	
	string lineCurr = String.Empty;//текущая строка
	
	int lineNumberCurr = 0;//номер текущей строки
		
	lock(SyncObjects.ListSyncer)
	{
		//первично читаем файл и заносим в список
		if(file == null)
		{
			file = File.ReadAllLines(pathFileOut).ToList();	
			header = file[0].Split(';').ToList();			
		}
		
		//сравнивнение счтчика количества строк с количеством строк в файле, если больше или равно, значит распарсил все данные
		if(lineNumber >= file.Count)
		{
			project.SendInfoToLog("Успешно обработали все строки в файле", true);
			break;
		}
		
		//присваивние счетчику текущей строки значение счетчика текущей строки в файле
		lineNumberCurr = lineNumber;
		//увеличивание счетчика текущей строки на 1
		lineNumber++;
		
		//получение текущей строки
		lineCurr = file[lineNumberCurr];
		
		project.SendInfoToLog(String.Format("Получили строку для парсинга - {0}", lineCurr), true);	
		
		//указание разделителя в массиве
		string[] checkLineCurr = lineCurr.Split(';');
		
		//проверка количества элементов в массиве, если больше 3 значит эту строку уже обработали
		if (checkLineCurr.Length > 3)
		{
			project.SendWarningToLog(String.Format("Строка {0} уже распарсена, берем следующую.", lineCurr),true);
			continue;
		}		
	}
	
	//указание разделителя в массиве
	string[] arrLineCurr = lineCurr.Split(';');
	
	dictLineCurr = new Dictionary <string, string>();
	
	//заполнение словаря текущими значениями, перебор заголовки
	for (int i = 0; i < header.Count; i++)
	{
		// Проверка числа элементов массива (имеющихся значений в строке) с текущим стобцом
		if (arrLineCurr.Count() > i)
		{
			//в стобце есть значение
			dictLineCurr.Add(header[i], arrLineCurr[i]);
			project.SendInfoToLog(String.Format("{0} - {1}", header[i], arrLineCurr[i].ToString()));
		}
		else
		{
			//в столбце нет значения
			dictLineCurr.Add(header[i], "");
		}		
	}
	
	//получение параметра LinkYelp со словаря
	string url = dictLineCurr["LinkYelp"];
	
	//флаг для прокси
	bool flagProxy = true;
	
	//проверка прокси
	for(int i = 0; i < proxy.Count; i++)
	{
		//получение строку
		string proxys = proxy[i];
		//запрос
		response = ZennoPoster.HttpGet(url, proxys, "utf-8", ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.HeaderAndBody, 25000, "");//, project.Profile.UserAgent, true, 5).HtmlDecode();
		
		//проверка заблочена ли прокси
		if(!response.Contains("Sorry, you’re not allowed to access this page."))
		{
			flagProxy = false;//перевод флага в состояние fale
			break;//прерывание цикл
		}
		
		project.SendWarningToLog("Прокси " + proxys + " в блоке. Берем следующую.", true);
	}
	
	//сверка флага, если не переведен в false значит все прокси в списке в блоке
	if(flagProxy)
	{
		throw new Exception("Все прокси в блоке. Прекращаем работу!");
	}
	
	//новый объект класса HtmlDocument
	HtmlDocument doc = new HtmlDocument();
	
	//загрузка ответа в объект doc
	doc.LoadHtml(response);
	
	//объявление ноды с именем компании
	HtmlNode elName = doc.DocumentNode.SelectSingleNode("//h1");
	
	//если нода  равна null вывод сообщение
	if (elName == null)
	{
		project.SendInfoToLog("Не найдено имя компании, проверьте парсер!");
		continue;
	}
	
	//парс сайт компании
	HtmlNode elSite = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'website')]/a");
	string webSite = "No";
	if(elSite != null)
		webSite = elSite.InnerText.Trim();
	
	//добавление сайт в словарь
	if(!dictLineCurr.ContainsKey("WebSite"))//проверка есть ли указанный ключ в словаре
		dictLineCurr.Add("WebSite", webSite);//если нет добавить и ключ и значение
	else
		dictLineCurr["WebSite"] = webSite;//если есть присваивоить значение указанному ключу
	
	project.SendInfoToLog(String.Format("Получили веб-сайт компании - {0}", webSite),true);	
	
	//парс номер телефона компании
	HtmlNode elTel = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'biz-phone')]");
	string tel = "No";
	if(elTel != null)
		tel = elTel.InnerText.Trim();

	project.SendInfoToLog(String.Format("Получили телефон компании - {0}", tel),true);	
	
	//добавление телефон в словарь
	if (!dictLineCurr.ContainsKey("Tel"))//проверка есть ли указанный ключ в словаре
		dictLineCurr.Add("Tel", tel);//если нет добавить и ключ и значение		
	else		
		dictLineCurr["Tel"] = tel;//если есть присваивоение значения указанному ключу
	
	//парс количества отзывов
	HtmlNode elRaiting = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'biz-rating biz-rating-very-large')]/span[contains(@class, 'review-count rating-qualifier')]");
	string raiting = "No";
	
	if(elRaiting != null)
		raiting = elRaiting.InnerText.Replace("reviews", "").Trim();
	
	//добавление количества отзывов в словарь
	if (!dictLineCurr.ContainsKey("Reviews"))//проверка есть ли указанный ключ в словаре
		dictLineCurr.Add("Reviews", raiting);//если нет добавить и ключ и значение		
	else		
		dictLineCurr["Reviews"] = raiting;//если есть присвоение значения указанному ключу
	
	//парс рейтинг
	HtmlNode elStars = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'biz-page')]//div[contains(@class, 'i-stars')]");
	string stars = "No";
	
	if(elStars != null)
	{
		stars = elStars.InnerHtml;
		stars = Regex.Match(stars, @"(?<=alt="").*(?=\ star)").Value;
	}
	
	//добавление рейтинга в словарь
	if (!dictLineCurr.ContainsKey("Raiting"))//проверка есть ли указанный ключ в словаре
		dictLineCurr.Add("Raiting", stars);//если нет добавление и ключа и значения		
	else		
		dictLineCurr["Raiting"] = stars;//если есть присваивить значение указанному ключу
	
	//переменная для формирования выходной строки
	string LineOut = "";
	
	//блокировка для формированя выходной строки
	lock(SyncObjects.ListSyncer)
	{
		//перебор списка заголовков, все имеющиеся ранее заголовки в таблице и добавление их значение в формируемую строку
		for (int i = 0; i< header.Count; i++)
		{
			//если словарь содержит столбец из таблицы - добавление значения в выходную строку
			if (dictLineCurr.ContainsKey(header[i]))
			{
				project.SendInfoToLog(header[i]);
				LineOut += dictLineCurr[header[i]] + ";";	
				project.SendInfoToLog(LineOut);
			}
			
		}
		
		//перебор словаря на не имеющиеся ранее заголовки в таблице и добавление их значение в формируемую строку
		for (int i = 0; i < dictLineCurr.Count; i++)
		{
			//если список заголовков не содержит заголовка, который есть в словаре - добавлевить новый заголовок в конец выходной строки
			if (!header.Contains(dictLineCurr.ElementAt(i).Key))
			{
				project.SendInfoToLog(dictLineCurr.ElementAt(i).Key);
				header.Add(dictLineCurr.ElementAt(i).Key);
				LineOut +=dictLineCurr.ElementAt(i).Value + ";";
				project.SendInfoToLog(LineOut);
			}
			
		}
		
		//сохранение данных обработанного товара
		countBlockLine++;
		//обновление списка заголовоков в файле из списка заголовков
		file[0] = String.Join(";", header);
		file[lineNumberCurr] = LineOut;
	}
	
	//сохранение результатоа в файл
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


