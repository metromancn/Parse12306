# 分析12306 获取全国列车数据
本程序介绍了如何从12306网站抓取全国高速列车数据。

* 开发工具：Visual Studio 2013
* 语言：C#
* 其他Library：Json.NET

## 概述
最近着手做全国高铁的App。  
计划绘制一张全国高铁车站的线路图，可以直接在图上点选，提供多种换乘和搜索方式等等。  
于是问题来了：“如何获取全国高速列车的数据”。  
本程序就是介绍如何从12306网站抓取全国高速列车的数据。项目包含了所有数据抓取和数据解析的C#源代码，以及最终做成的Excel文档。

之前做过全国地铁的App：“地铁通-MetroMan”。  
[iOS下载](https://itunes.apple.com/cn/app/de-tie-tong-metroman/id466351433?mt=8)　　[Android下载](https://play.google.com/store/apps/details?id=com.xinlukou.metroman&hl=zh)  

后续会将地铁App和高铁App全部开源出来。  


## 具体步骤
1. 从12306下载车站信息
2. 解析车站信息
3. 从12306下载车次信息
4. 解析车次信息
5. 根据车次和车站解析时刻表URL
6. 从12306下载时刻表信息
7. 解析时刻表信息
8. 将车站车次和时刻表信息做成Excel易读格式

### 1. 从12306下载车站信息
通过分析12306的网站代码，发现[全国车站信息的URL](https://kyfw.12306.cn/otn/resources/js/framework/station_name.js)

```
https://kyfw.12306.cn/otn/resources/js/framework/station_name.js
```

### 2. 解析车站信息
解析1的数据，输出成以下格式

```
ID  电报码  站名    拼音        首字母  拼音码
0   BOP    北京北  beijingbei  bjb   bjb
```

### 3. 从12306下载车次信息
通过分析12306的网站代码，发现[全国车次信息的URL](https://kyfw.12306.cn/otn/resources/js/query/train_list.js)。这个文件存储了当前60天的所有车次信息，大约有35MB。

```
https://kyfw.12306.cn/otn/resources/js/query/train_list.js
```

### 4. 解析车次信息
解析3的数据，按照日期分割成以下格式。

```
类型  列车编号       车次  起点  终点
D    24000000D10R  D1   北京  沈阳
```
12306将全国列车分成了7类，C-城际高速，D-动车，G-高铁，K-普快，T-特快，Z-直达，O-其他列车。这里我们仅抽取 C-城际高速，D-动车，G-高铁 的数据。

### 5. 根据车次和车站解析时刻表URL
首先Merge所有日期的车次，以车次和列车编号为KEY，去除重复后得到全部车次一览。  
然后根据各车站的电报码，得出下载时刻表用的URL。如下：

```
https://kyfw.12306.cn/otn/czxx/queryByTrainNo?train_no=列车编号&from_station_telecode=出发车站电报码&to_station_telecode=到达车站电报码&depart_date=出发日期
```

#####注意点
a) 部分车次仅在特定日期运营（比如:工作日，周末，节假日等）  
b) 同一车次，在不同日期，运营时刻和停靠车站可能不一样  
c) 同一车次同一列车编号，在不同日期，运营时刻和停靠车站完全一致  

### 6. 从12306下载时刻表信息
根据步骤5中得到的时刻表URL，下载所有时刻表信息。（JSON格式）

### 7. 解析时刻表信息
解析6的数据，分别输出完整的“车站”，“车次”，“时刻表”成以下CSV格式

```
ID  电报码  站名    拼音        首字母  拼音码
0   BOP    北京北  beijingbei  bjb   bjb
```

```
车次   起点   终点 出发时间  到达时间 类别  服务
C1002 延吉西 长春  5：47    8：04   动车  2
```

```
车次   站序  站名   到站时间  出发时间  停留时间  是否开通
C1002 1    延吉西  ----    6:20     ----     TRUE
      2    长春    8:25    8:25     ----     TRUE
```

### 8. 将车站车次和时刻表信息做成Excel易读格式
由于步骤7的数据已经是CSV格式，可以直接粘贴到Excel里面。
步骤1--步骤7的中间文件没有放到项目文件夹里面。
步骤8的Excel文件可以在项目文件夹中找到。“全国高速列车时刻表_20160310.xlsx”

## 祝你好运
玩得开心，也请记得给我反馈。如果您发现了什么 bug (这简直是必然的)，请直接指出，如果还能附带一个 pull request 修正的话，那真的感激万分！

欢迎加颗星星或者 follow 我一下以示支持，这将对我和我的项目的发展提供不可估量的帮助。再次感谢。

E-Mail : metromancn@gmail.com

## 许可
MIT License