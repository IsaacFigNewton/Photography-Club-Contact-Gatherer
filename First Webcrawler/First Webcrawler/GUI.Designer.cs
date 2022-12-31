using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IronXL;
using System.Net;
using System.IO;

/*
 * Agenda:
 * **************************************************************************************************************************************************
 * The code around line 720, where the parseContactWithKeywordLocation method is called, needs to parse the correct info, not necessarily the first contact info. (Maybe compile a list of possible contact info and then go through them, checking validity somehow?)
 * See line 482 and fix it so that it doesn't take ~2 hours to do 20 entries
 * When the crawler reads source URLs from workbook, make it so it reads hyperlinks, then the text of a hyperlink is unavailable, see IronXL docs for help: https://ironsoftware.com/csharp/excel/object-reference/api/IronXL.Cell.html
 * Step 3 gives the URL indices of url's instead of row numbers and step 3 needs to stop after reading to the number of entries
 * Integrate the UI
 * Refactor code to convert CONTACTS_PAGE_SEARCH_KEYWORDS to 2D ArrayLists or the C# analog
 * Refactor code to give checkBoxIsChecked and contactInfo dynamic lengths (modifiable by GUI events, such as changing gathering method)
 * Find a valid alternative to the methods dealing with scraping Google and Facebook other than using their APIs (which cost $)
 * Refine contact search keywords after previous stuff to make searches more accurate
*/

namespace First_Webcrawler
{
    public partial class GUI : Form
    {

        //                                                                  File Reading Class Variables
        //Row counting vars
        public static int NUMBER_OF_ENTRIES = 20;
        //default offset should be 1, for headers
        public static int rowOffset = 1;
        //set URLIndex to rowOffset so that's where it starts
        public static int URLIndex = 0;

        //Row/column reading vars
        public static int NAMES_COLUMN = 1;
        public static int READING_COLUMN = 1;
        public static String MAIN_URL_WRITING_COLUMN = "G"; //K
        public static String CONTACT_URL_WRITING_COLUMN = "F"; //L
        //email = cost
        public static String EMAIL_WRITING_COLUMN = "E"; //E
        //phone = yes or no lodging
        public static String PHONE_WRITING_COLUMN = "H"; //F
        //address = destination
        public static String ADDRESS_WRITING_COLUMN = "C"; //C
        //other = available
        public static String OTHER_WRITING_COLUMN = "D"; //A

        //File path vars
        public static String SHEET_NAME = "Sheet1";
        public static String NAME_OF_IO_DOC = "Hyperlink to URL to Info.xlsx";
        public static String PATH_OF_IO_DOC = "..//" + NAME_OF_IO_DOC;



        //                                                                  URL Gathering Class Variables
        //Main URL vars
        //Empty cells not included in list of sites
        public static string[] URLs = new String[NUMBER_OF_ENTRIES];

        //Contact URL vars
        public static string[] contactURLs = new String[NUMBER_OF_ENTRIES];
        //when you inevitably increase the number of things in the array below, you'll have to make the one below it a 2D array and alter lines 137-145 to allow subsequent known URLs
        public static string[] KNOWN_CONTACT_URLS = { "https://www.facebook.com/" };
        public static String[] LINK_SEARCH_TAGS_START = { "<a ", "<button ", "< a ", "< button " };
        public static String[] LINK_SEARCH_TAGS_END = { ">", "/>", "</" };
        //all lowercase to expedite searches
        public static String[] LINK_SEARCH_KEYWORDS = { "contact", "about", "meet", "join", "board", "coordinators" };
        public static string[] KNOWN_CONTACT_URLS_LOCATOR_KEYPHRASES = { "web address:- <a href=", "" };
        public static string[] CONTACT_URL_PARSING_KEYWORDS = { "href=", "src=" };

        //For dictionary guessing contact page URLs or going to local URLs
        //if the website's link goes to something including one of the following phrases(paired with an extension from the URL_TYPE_EXTENSIONS list), just remove it and then brute force the contact URL with the following extension words/phrases
        public static String[] URL_REMOVE_EXTENSIONS = { "Index", "index", "Home", "home", "Default", "default", "Welcome", "welcome" };
        //in case the scraper can't get the contact page for whatever reason, use the below information to brute force the contact URL
        public static String[] URL_PRE_EXTENSIONS = { "/", "#", "about/", "Club/", "info/", "page/" };
        public static String[] URL_MAIN_EXTENSION = { "contact", "Contact", "about", "About", "join" };
        public static String[] URL_EXTENSION1 = { "", "-", "_",};
        public static String[] URL_EXTENSION2 = { "", "us", "Us", "form" };
        public static String[] URL_EXTENSION3 = { "", "2" };
        public static String[] URL_TYPE_EXTENSIONS = { "", ".html", ".htm", ".aspx", ".php", ".shtml", ".asp" };
        //if unable to find contact page this way, look for facebook link, go there, and then append "about"



        //                                                                  Contact Gathering Class Variables
        //Contact info storage vars
        //# of types of contact information in the array below
        public static int NUMBER_OF_CONTACT_TYPES = 4;
        //2-dimensional array of contact info in String form
        //ex: int[,] array2D = new int[,] { {email1, phone1, other1}, {email2, phone2, other2}, {email3, phone3, other3}};
        public static String[,] contactInfo = new String[NUMBER_OF_ENTRIES, NUMBER_OF_CONTACT_TYPES];

        //Contact detection vars
        // [0] = Email, [1] = Phone, [2] = Address, [3] = Other
        public static Boolean[] checkBoxIsChecked = { true, true, true, true };
        public static Boolean endOfBody = false;
        //maximum search keyword length to tell when to stop when looking for contact keywords
        public static int MAX_CONTACT_KEYWORD_LENGTH = 8;
        //Contact phrase html segment (for debugging porpoises only)
        public static int CONTACT_SEGMENT_SIZE = 100;
        public static String impossibleSearchPhrase = "Hopefully this will never show up in a site's HTML";

        //Contact info searching vars
        //# of keyword items in each array within the array of contact keywords above
        public static int NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS = 4;
        //the next link or info-specific indicator should be searched for after one of these search keywords is found
        //the below array can be empty because it's "filled" by the resetContactsSearchKeywordsArray(); method
        public static String[,] CONTACTS_PAGE_SEARCH_KEYWORDS = new String[NUMBER_OF_CONTACT_TYPES, NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS];
        //# of keyword items in each array within the array of contact keywords above
        public static int NUMBER_OF_CONTACT_PARSING_KEYWORDS = 4;
        //the below array is used to correctly parse the different types of contact information found by the above array
        public static String[,] CONTACT_PARSING_KEYWORDS_START = new String[NUMBER_OF_CONTACT_TYPES, NUMBER_OF_CONTACT_PARSING_KEYWORDS];
        //the below array is used to correctly parse the different types of contact information found by the above array
        public static String[,] CONTACT_PARSING_KEYWORDS_END = new String[NUMBER_OF_CONTACT_TYPES, NUMBER_OF_CONTACT_PARSING_KEYWORDS];


        //                                                                               End of class variable instantiation, beginning of GUI event assignment and scary code
        //****************************************************************************************************************************************************************************************
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public GUI()
        {
            InitializeComponent();
            buttonScrapeGoogle.Click += new EventHandler(this.buttonScrapeGoogle_Click);
            buttonScrapeURLs.Click += new EventHandler(this.buttonScrapeURLs_Click);
            checkBoxEmail.CheckedChanged += new EventHandler(this.checkBoxEmail_CheckedChanged);
            checkBoxPhone.CheckedChanged += new EventHandler(this.checkBoxPhone_CheckedChanged);
            checkBoxAddress.CheckedChanged += new EventHandler(this.checkBoxAddress_CheckedChanged);
            checkBoxOther.CheckedChanged += new EventHandler(this.checkBoxOther_CheckedChanged);
            buttonLoadURLs.Click += new EventHandler(this.buttonGetURLs_Click);
            buttonLocateContacts.Click += new EventHandler(this.buttonLocateContacts_Click);
            buttonReadSites.Click += new EventHandler(this.buttonReadSites_Click);
            buttonWriteContacts.Click += new EventHandler(this.buttonWriteContacts_Click);
            //add way for people to open workbook without having to edit the code
            //PATH_OF_IO_DOC = this.openFileDialog1.
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code


        //                                                                               End of GUI event assignment and scary code, beginning of GUI updating area
        //****************************************************************************************************************************************************************************************

        private void resetContactsSearchKeywordsArray()
        {
            //set all CONTACTS_PAGE_SEARCH_KEYWORDs to their "off" states
            for (int i = 0; i < NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS; i++)
                for (int j = 0; j < NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS; j++)
                    CONTACTS_PAGE_SEARCH_KEYWORDS[i, j] = impossibleSearchPhrase;

            //alter respective email search word entries
            if (checkBoxIsChecked[0])
            {
                CONTACTS_PAGE_SEARCH_KEYWORDS[0, 0] = "Email";
                CONTACTS_PAGE_SEARCH_KEYWORDS[0, 1] = "email";
                CONTACTS_PAGE_SEARCH_KEYWORDS[0, 2] = "mailto:";
                CONTACTS_PAGE_SEARCH_KEYWORDS[0, 3] = "@";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[0, 0] = "$";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[0, 1] = "price";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[0, 2] = "cost";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[0, 3] = "per person";
            }

            //alter respective phone search word entries
            if (checkBoxIsChecked[1])
            {
                CONTACTS_PAGE_SEARCH_KEYWORDS[1, 0] = "Phone";
                CONTACTS_PAGE_SEARCH_KEYWORDS[1, 1] = "phone";
                CONTACTS_PAGE_SEARCH_KEYWORDS[1, 2] = "tel:-";
                CONTACTS_PAGE_SEARCH_KEYWORDS[1, 3] = "1(";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[1, 0] = "lodging";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[1, 1] = "occupancy";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[1, 2] = "overnight";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[1, 3] = "stay";
            }

            //alter respective address search word entries
            if (checkBoxIsChecked[2])
            {
                CONTACTS_PAGE_SEARCH_KEYWORDS[2, 0] = "Address";
                CONTACTS_PAGE_SEARCH_KEYWORDS[2, 1] = "address";
                CONTACTS_PAGE_SEARCH_KEYWORDS[2, 2] = "Location";
                CONTACTS_PAGE_SEARCH_KEYWORDS[2, 3] = "location";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[2, 0] = "yellowstone";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[2, 1] = "glacier national park";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[2, 2] = "colorado";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[2, 3] = "coast";
            }

            //alter respective other search word entries
            if (checkBoxIsChecked[3])
            {
                CONTACTS_PAGE_SEARCH_KEYWORDS[3, 0] = "meet";
                CONTACTS_PAGE_SEARCH_KEYWORDS[3, 1] = "Ave";
                CONTACTS_PAGE_SEARCH_KEYWORDS[3, 2] = "Rd";
                CONTACTS_PAGE_SEARCH_KEYWORDS[3, 3] = "Ln";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[3, 0] = "2020";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[3, 1] = "2019";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[3, 2] = "2018";
                //CONTACTS_PAGE_SEARCH_KEYWORDS[3, 3] = "2017";
            }

            ////alter respective other search word entries
            //if (checkBoxIsChecked[4])
            //{
            //    CONTACTS_PAGE_SEARCH_KEYWORDS[4, 0] = "meet";
            //    CONTACTS_PAGE_SEARCH_KEYWORDS[4, 1] = "Ave";
            //    CONTACTS_PAGE_SEARCH_KEYWORDS[4, 2] = "Rd";
            //    CONTACTS_PAGE_SEARCH_KEYWORDS[4, 3] = "Ln";
            //    //CONTACTS_PAGE_SEARCH_KEYWORDS[4, 0] = "maximum";
            //    //CONTACTS_PAGE_SEARCH_KEYWORDS[4, 1] = "participants";
            //    //CONTACTS_PAGE_SEARCH_KEYWORDS[4, 2] = "size";
            //    //CONTACTS_PAGE_SEARCH_KEYWORDS[4, 3] = "group";
            //}

            //set all CONTACTS_PAGE_SEARCH_KEYWORDs to lowercase
            for (int i = 0; i < NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS; i++)
                for (int j = 0; j < NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS; j++)
                    CONTACTS_PAGE_SEARCH_KEYWORDS[i, j] = CONTACTS_PAGE_SEARCH_KEYWORDS[i, j].ToLower();

            //print updated keyword arrays
            Console.WriteLine("");
            for (int i = 0; i < NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS; i++)
            {
                //if the checkbox in question is unchecked, print to the console that it's unchecked
                if (!checkBoxIsChecked[i])
                    Console.WriteLine("Checkbox number " + (i + 1) + " is unchecked." + "\n");
                //if for whatever reason the list hasn't been updated yet, say so
                else if (CONTACTS_PAGE_SEARCH_KEYWORDS[i, 0] == null)
                    Console.WriteLine("Checkbox number " + (i + 1) + " is somehow not updated, or is broken." + "\n");
                //otherwise the entry is considered to be checked
                else
                    Console.WriteLine("Checkbox number " + (i + 1) + " is checked." + "\n");
            }

            //set the values for the CONTACT_PARSING_KEYWORDS_START array
            //email
            CONTACT_PARSING_KEYWORDS_START[0, 0] = " ";
            CONTACT_PARSING_KEYWORDS_START[0, 1] = "mailto:";
            CONTACT_PARSING_KEYWORDS_START[0, 2] = "\"";
            CONTACT_PARSING_KEYWORDS_START[0, 3] = ">";
            //phone
            CONTACT_PARSING_KEYWORDS_START[1, 0] = " ";
            CONTACT_PARSING_KEYWORDS_START[1, 1] = "1(";
            CONTACT_PARSING_KEYWORDS_START[1, 2] = "\"";
            CONTACT_PARSING_KEYWORDS_START[1, 3] = ">";
            //address
            CONTACT_PARSING_KEYWORDS_START[2, 0] = " ";
            CONTACT_PARSING_KEYWORDS_START[2, 1] = ":";
            CONTACT_PARSING_KEYWORDS_START[2, 2] = "\"";
            CONTACT_PARSING_KEYWORDS_START[2, 3] = ">";
            //other
            CONTACT_PARSING_KEYWORDS_START[3, 0] = " ";
            CONTACT_PARSING_KEYWORDS_START[3, 1] = ":";
            CONTACT_PARSING_KEYWORDS_START[3, 2] = "\"";
            CONTACT_PARSING_KEYWORDS_START[3, 3] = ">";

            //set the values for the CONTACT_PARSING_KEYWORDS_END array
            //email
            CONTACT_PARSING_KEYWORDS_END[0, 0] = "\"";
            CONTACT_PARSING_KEYWORDS_END[0, 1] = "<";
            CONTACT_PARSING_KEYWORDS_END[0, 2] = "?subject=";
            CONTACT_PARSING_KEYWORDS_END[0, 3] = " ";
            //phone
            CONTACT_PARSING_KEYWORDS_END[1, 0] = "\"";
            CONTACT_PARSING_KEYWORDS_END[1, 1] = "<";
            CONTACT_PARSING_KEYWORDS_END[1, 2] = "This should never show up in a website's HTML";
            CONTACT_PARSING_KEYWORDS_END[1, 3] = "This should never show up in a website's HTML";
            //address
            CONTACT_PARSING_KEYWORDS_END[2, 0] = "\"";
            CONTACT_PARSING_KEYWORDS_END[2, 1] = "<";
            CONTACT_PARSING_KEYWORDS_END[2, 2] = "This should never show up in a website's HTML";
            CONTACT_PARSING_KEYWORDS_END[2, 3] = "This should never show up in a website's HTML";
            //other
            CONTACT_PARSING_KEYWORDS_END[3, 0] = "\"";
            CONTACT_PARSING_KEYWORDS_END[3, 1] = "<";
            CONTACT_PARSING_KEYWORDS_END[3, 2] = "This should never show up in a website's HTML";
            CONTACT_PARSING_KEYWORDS_END[3, 3] = "This should never show up in a website's HTML";

        }

        //Added contact search keyphrase changing functionality

        private void checkBoxEmail_CheckedChanged(object sender, EventArgs e)
        {
            //reset the check state
            checkBoxIsChecked[0] = checkBoxEmail.Checked;
            //checkBoxEmail.Checked = !checkBoxEmail.Checked;

            resetContactsSearchKeywordsArray();

            Console.WriteLine("Changed CONTACTS_PAGE_SEARCH_KEYWORDS");
        }

        private void checkBoxPhone_CheckedChanged(object sender, EventArgs e)
        {
            //reset the check state
            checkBoxIsChecked[1] = checkBoxPhone.Checked;
            //checkBoxPhone.Checked = !checkBoxPhone.Checked;

            resetContactsSearchKeywordsArray();

            Console.WriteLine("Changed CONTACTS_PAGE_SEARCH_KEYWORDS");
        }

        private void checkBoxAddress_CheckedChanged(object sender, EventArgs e)
        {
            //reset the check state
            checkBoxIsChecked[2] = checkBoxAddress.Checked;
            //checkBoxOther.Checked = !checkBoxOther.Checked;

            resetContactsSearchKeywordsArray();

            Console.WriteLine("Changed CONTACTS_PAGE_SEARCH_KEYWORDS");
        }

        private void checkBoxOther_CheckedChanged(object sender, EventArgs e)
        {
            //reset the check state
            checkBoxIsChecked[3] = checkBoxOther.Checked;
            //checkBoxOther.Checked = !checkBoxOther.Checked;

            resetContactsSearchKeywordsArray();

            Console.WriteLine("Changed CONTACTS_PAGE_SEARCH_KEYWORDS");
        }

        //                                                              End of GUI updating area, beginning of source URL scraping section
        //*****************************************************************************************************************************************************************************************

        private void buttonScrapeGoogle_Click(object sender, EventArgs e)
        {
            //read the URLs from the excel doc to an array of strings
            WorkBook workbook = WorkBook.Load(PATH_OF_IO_DOC);
            WorkSheet worksheet = workbook.GetWorkSheet(SHEET_NAME);

            int rowCount = NUMBER_OF_ENTRIES + rowOffset;
            int offset = rowOffset;
            //                                                                     Get the HTML starting at where the search results should be
            String[] sourceURLs = getURLsFromHTML(getHTML(assembleGoogleURL(false).Substring(0, getHTML(assembleGoogleURL(false)).Length - 0)), LINK_SEARCH_TAGS_START); //Replace the url within with the 



            for (int i = offset; i < rowCount; i++)
            {
                //check to make sure correct values for correct column are written
                Console.WriteLine(i - offset);

                //set value by row and column indexing
                worksheet[MAIN_URL_WRITING_COLUMN + i].Value = sourceURLs[i - offset];
                Console.WriteLine(sourceURLs[i - offset]);

                Console.WriteLine("");
            }

            //save the altered workbook
            workbook.Save();
            Console.WriteLine("Finished scraping and writing source URLs to document");
            Console.WriteLine("");

        }

        //                                                              End of source URL scraping section, beginning of scraping from urls section
        //*****************************************************************************************************************************************************************************************

        private void buttonScrapeURLs_Click(object sender, EventArgs e)
        {
            //read the URLs from the excel doc to an array of strings
            WorkBook workbook = WorkBook.Load(PATH_OF_IO_DOC);
            WorkSheet worksheet = workbook.GetWorkSheet(SHEET_NAME);

            //make sure to instantiate the contact search keywords array if it's not already been instantiated by changing the check state of a checkbox
            resetContactsSearchKeywordsArray();

            int rowCount = NUMBER_OF_ENTRIES + rowOffset;
            //start at rowOffset to skip the header
            for (int i = rowOffset; i < rowCount; i++)
            {
                //get value by row and column indexing
                string index_val = worksheet.Rows[i].Columns[READING_COLUMN].ToString();

                ////check to make sure correct values are collected
                //Console.WriteLine(i + "'{0}'", index_val);

                //scrape the info. from the url
                getContactsFromURL(index_val);
            }
            Console.WriteLine("Finished scraping URLs");
            Console.WriteLine("");

        }

        //                                                              End of scraping from URLs section, beginning of source url gathering section
        //*****************************************************************************************************************************************************************************************

        private void buttonGetURLs_Click(object sender, EventArgs e)
        {
            //read the URLs from the excel doc to an array of strings
            WorkBook workbook = WorkBook.Load(PATH_OF_IO_DOC);
            WorkSheet worksheet = workbook.GetWorkSheet(SHEET_NAME);

            int rowCount = NUMBER_OF_ENTRIES + rowOffset;
            //start at rowOffset to skip the header
            for (int i = rowOffset; i < rowCount; i++)
            {
                //get value by cell address
                //string address_val = ws["A" + rowCount].ToString();
                //get value by row and column indexing
                string index_val = worksheet.Rows[i].Columns[READING_COLUMN].ToString();

                //read each cell's value to the array of URLs
                URLs[i - rowOffset] = index_val;
                //set the URL to an empty string if it's originally a null value
                if (URLs[i - rowOffset] == null)
                    URLs[i - rowOffset] = "";

                //check to make sure correct values are collected
                Console.WriteLine(i + "'{0}'", index_val);
            }
            Console.WriteLine("Finished getting site URLs");
            Console.WriteLine("");

        }

        //                                                              End of source url gathering section, beginning of main page and contact page url scraping section
        //*****************************************************************************************************************************************************************************************

        private void buttonLocateContacts_Click(object sender, EventArgs e)
        {
            //make sure to instantiate the contact search keywords array if it's not already been instantiated by changing the check state of a checkbox
            resetContactsSearchKeywordsArray();

            URLIndex = 0;
            try
            {
                //loop through all the entries
                while (URLIndex < NUMBER_OF_ENTRIES)
                {
                    string url = URLs[URLIndex];
                    string html = getHTML(url);
                    String[] urlSearchPhrases = {""};

                    //make sure the url is valid
                    if (!(url == null || url == "") && (url.Length > 0))
                    {
                        urlSearchPhrases[0] = KNOWN_CONTACT_URLS_LOCATOR_KEYPHRASES[0];

                        //Show the webpage currently being read
                        Console.WriteLine("");
                        Console.WriteLine(URLIndex + rowOffset);
                        Console.WriteLine("What a strange new site to see!");
                        //since it's an unknown, and thus, assumed to be the main page of the target, searching for the URL isn't necessary, as we already have it
                        Console.WriteLine("Main page URL = " + URLs[URLIndex]);

                        //try to get the contact page URL specifically, and if unavailable, then try to get another likely contact page URL
                        //set the contacts page URL to the one found in the new webpage's HTML
                        contactURLs[URLIndex] = parseItemFromList(LINK_SEARCH_KEYWORDS, CONTACT_URL_PARSING_KEYWORDS, getURLsFromHTML(getHTML(URLs[URLIndex]), LINK_SEARCH_TAGS_START));
                        //if the scraped contact page URL doesn't have the keyword "contact" in it
                        if (contactURLs[URLIndex].ToLower().LastIndexOf("contact") < 0)
                        {
                            //instead of brute-forcing things, try visiting the robots.txt page or the sitemap.xml page if available, otherwise, give up, don't let it take 20 minutes/entry

                            //try the brute force method, stopping when it reaches the list entry after the last version of the "contact" keyword
                            String bruteForcedURL = tryBruteForce(URLs[URLIndex], 2);
                            //if the brute force method returns a url that contains the keyword, use that as the contact page url
                            if (bruteForcedURL.ToLower().LastIndexOf("contact") > 0)
                                contactURLs[URLIndex] = bruteForcedURL;


                            //otherwise leave it as is and accept that you can't get 'em all right
                        }

                        Console.WriteLine("Contact page URL = " + contactURLs[URLIndex]);

                        Console.WriteLine("");
                    }
                    else
                    {
                        Console.WriteLine("Empty source URL");
                        Console.WriteLine("");

                        //i think I'm doing this bit right
                        URLs[URLIndex] = assembleGoogleURL(true);
                        contactURLs[URLIndex] = URLs[URLIndex];
                    }
                    //increment the starting URLindex after an exception is seen so that it is incremented properly in the exception handlers
                    URLIndex++;
                }

                Console.WriteLine("");
                Console.WriteLine("Finished getting sites' URLs");
                Console.WriteLine("");
            }
            ////catch the null argument exception and let user try again, starting at the next URL
            //catch (ArgumentNullException ex)
            //{
            //    Console.WriteLine("Null Argument Exception caught, try again.");
            //    Console.WriteLine("");
            //    contactURLs[URLIndex] = parseItemFromList(LINK_SEARCH_KEYWORDS, CONTACT_URL_PARSING_KEYWORDS, getURLsFromHTML("", LINK_SEARCH_TAGS_START);
            //    URLIndex++;
            //}
            //catch the web exception and let user start again, starting at the next URL
            catch (WebException)
            {
                Console.WriteLine("WebException caused by url being: " + contactURLs[URLIndex]);
                contactURLs[URLIndex] = parseItemFromList(LINK_SEARCH_KEYWORDS, CONTACT_URL_PARSING_KEYWORDS, getURLsFromHTML("", LINK_SEARCH_TAGS_START));
                URLIndex++;
                Console.WriteLine("");
            }
        }

        private static string getHTML(string url)
        {
            try
            {
                //make sure that the url provided isn't empty
                if (url != null)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "C# console client";

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();  // can't do .ToLower(); because it ruins the urls, I've tested it
                        /*
                            maybe return 2 strings one of the standard and one of the .ToLower-ed html, so you can search the .ToLower-ed HTML
                            and then go to that character in the unaltered html for the url?
                            that's just for later on, when efficiency and not debugging is my main focus
                         */
                    }
                //if the url provided was null, just return an empty string
                } else {
                    Console.WriteLine("the url I was given was null");
                    return "";
                }
            }
            catch (WebException)
            {
                //if the URL is invalid, try googling the invalid url string maybe?
                Console.WriteLine("couldn't resolve the site host name because the url is '" + url + "'");
                return "";
            }
            catch (NotSupportedException)
            {
                //if the URL is invalid, try googling the invalid url string

                Console.WriteLine("exception caused by url being '" + url + "'");
                Console.WriteLine("");

                return "";
            }
            catch (UriFormatException)
            {
                Console.WriteLine("exception caused by url being '" + url + "'");
                Console.WriteLine("");

                return "";
            }
        }

        private static String [] getURLsFromHTML(string html, string [] searchTagsStart)
        {
            try {
            string[] links = getHTMLSegments(html, searchTagsStart, LINK_SEARCH_TAGS_END);

            Console.WriteLine("Found " + links.Length + " links");
            Console.WriteLine("first 3 values of links array:");
            Console.WriteLine(links[0] + ", " + links[1] + ", " + links[2]);

            //Reset endOfBody so that other methods can reuse the variable
            endOfBody = false;

            //return the url
            return links;
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("The links array was too small to print the first 3 links from it");
                Console.WriteLine("");

                //return the url
                return new string[10];
            }
        }


        //Note: You can't break search for contacts from html SEGMENTS because contact info isn't virtually always contained in an "a" tag or a "button" tag like contact page urls are
        public static string[] getHTMLSegments (string html, String[] searchTagsStart, String[] searchTagsEnd)
        {
            try
            {
                //initialize a string array of empty strings
                string[] segments = new string[10000];
                //initialize segments list
                for (int i = 0; i < segments.Length; i++)
                    segments[i] = "empty";

                if (html.Length > 0)
                {
                    int segmentCounter = 0;
                    endOfBody = false;

                    //print first few chars of HTML as indication of proper functioning
                    Console.WriteLine(html.Substring(0, 15));

                    //for counting segments
                    int l = 0;
                    //index for finding the starting spot of each link
                    int i = 0;
                    bool foundContact = false;
                    //read through html until it reaches the end of the body or finds the contact
                    while (!endOfBody && i < html.Length - 8 && !foundContact && segmentCounter < segments.Length)
                    {
                        //read through all of the search keywords
                        for (int j = 0; j < searchTagsEnd.Length; j++)
                        {
                            //makes a list (segments array) of <a> blocks (<a> and <button> blocks) to go through and store them in the segments array
                            //if the site's HTML includes the keywords somewhere, look nearby it for the URL of the contacts page
                            if (j < searchTagsStart.Length && i < html.Length - searchTagsStart[j].Length && html.Substring(i, searchTagsStart[j].Length) == (searchTagsStart[j]))
                            {
                                //i is the starting spot of each link and k is the ending spot

                                //find the <a> and <button> blocks
                                //look through subsequent html for closing <a> or <button> tag, and when it's found, set k to it
                                int k = i + 0;
                                Console.WriteLine("ArgumentOutOfRange exception caused by the length parameter of the substring portion of the line of code on Line 646 and is for some reason specific to some sites");
                                while (j < searchTagsEnd.Length && html.Substring(k, searchTagsEnd[j].Length) != (searchTagsEnd[j]))
                                    k++;

                                //add another link as an entry to the array of segments to go through
                                //this is not the cause of null value exceptions either
                                if (html.Substring(i, k - i) != null && html.Substring(i, k - i) != "")
                                    segments[segmentCounter] = html.Substring(i, k - i);
                                segmentCounter++;

                                //debugging
                                l++;
                                //Console.WriteLine("Found a link at character #" + i);
                                //if (i - CONTACT_SEGMENT_SIZE >= 0)
                                //    Console.WriteLine(html.Substring(i - CONTACT_SEGMENT_SIZE, 2 * CONTACT_SEGMENT_SIZE + searchTagsStart[j].Length));
                                //else
                                //    Console.WriteLine(html.Substring(0, CONTACT_SEGMENT_SIZE + searchTagsStart[j].Length));
                            }

                            //move on to the next HTML once it's finished reading through this HTML
                            if ((html.Substring(i, 7) == ("</body>")) || i == html.Length - 1)
                            {
                                endOfBody = true;
                                break;
                            }
                        }

                        i++;
                    }
                    return segments;
                }
                else
                {
                    Console.WriteLine("Somehow there was no html at this URL");
                    return segments;
                }
            } catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("ArgumentOutOfRange exception caused by the length parameter of the substring portion of the line of code on Line 646 and is for some reason specific to some sites");
                return new string[10];
            }
        }

        //                                                              End of main page and contact page url scraping section, beginning of contact scraping section
        //*****************************************************************************************************************************************************************************************

        private void buttonReadSites_Click(object sender, EventArgs e)
        {
            //read the information on the new site URL
            //basically the same as buttonLocateContacts_Click(), but it stores the contact data collected
            try
            {
                //Reset URLIndex so that the while loop isn't just immediately skipped because URLIndex == NUMBER_OF_ENTRIES at the start of this method
                URLIndex = 0;
                //Reset endOfBody so that other methods can reuse the variable (maybe unnecessary at the beginning of this method, but you can never be too sure)
                endOfBody = false;


                while (URLIndex < NUMBER_OF_ENTRIES)
                {
                    Console.WriteLine("");
                    Console.WriteLine(URLIndex + rowOffset);

                    string url = contactURLs[URLIndex];

                    //make sure the url is defined
                    if (url != null && url != "")
                        getContactsFromURL(url);
                    
                    Console.WriteLine("");
                    //increment the starting URLindex after an exception is seen so that it is incremented properly in the exception handlers
                    URLIndex++;
                }

                Console.WriteLine("");
                Console.WriteLine("Finished getting sites' contact information");
                Console.WriteLine("");
            }
            ////catch the null argument exception and let user try again, starting at the next URL
            //catch (ArgumentNullException ex)
            //{
            //    Console.WriteLine(URLIndex + rowOffset);
            //    Console.WriteLine("Null Argument Exception caught, try again.");
            //    getContactsFromURL("");
            //    URLIndex++;
            //}
            //catch the web exception and let user start again, starting at the next URL
            catch (WebException)
            {
                Console.WriteLine(URLIndex + rowOffset);
                Console.WriteLine("WebException caused by URL being '" + contactURLs[URLIndex] + "'");
                getContactsFromURL(contactURLs[URLIndex]);
                URLIndex++;
            }
        }

        //Note: You can't break search for contacts from html SEGMENTS because contact info isn't virtually always contained in an "a" tag or a "button" tag like contact page urls are
        private static void getContactsFromURL(string url)
        {
            try
            {
                //Reset endOfBody so that other methods can reuse the variable (maybe unnecessary at the beginning of this method, but you can never be too sure)
                endOfBody = false;

                //make sure the url is not empty
                if (url != "")
                {
                    string html = getHTML(url);
                    //all lowercase for searching porpoises
                    string lowercaseHTML = html.ToLower();

                    //make sure the input is not empty
                    if (html != "")
                    {
                        //print first 4 chars of HTML as indication of proper functioning
                        Console.WriteLine(html.Substring(0, 15));

                        //the index of some character in the HTML
                        int i = 0;
                        //read through html until it gets to the end of the html or body
                        while (!(i >= html.Length - MAX_CONTACT_KEYWORD_LENGTH || endOfBody))
                        {
                            //read through all of CONTACTS_PAGE_SEARCH_KEYWORDS arrays
                            for (int j = 0; j < NUMBER_OF_CONTACT_TYPES; j++)
                            {
                                //skip values of j that correspond to unwanted contact types (==false instead of ! for purposes of legibility)
                                if (j == 0 && checkBoxIsChecked[0] == false)
                                    j++;
                                if (j == 1 && checkBoxIsChecked[1] == false)
                                    j++;
                                if (j == 2 && checkBoxIsChecked[2] == false)
                                    j++;
                                if (j == 3 && checkBoxIsChecked[3] == false)
                                    break;

                                //read through the items of the arrays of the array (ex, CONTACTS_PAGE_SEARCH_KEYWORDS array 1, item 3)
                                for (int k = 0; k < NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS; k++)                                         //problem here
                                {
                                    //if the site's HTML includes the keywords somewhere, look nearby it for the contacts information
                                    //IndexOutOfRangeException in the below substring statement because you need to update NUMBER_OF_CONTACT_TYPES and NUMBER_OF_CONTACT_PAGE_SEARCH_KEYWORDS
                                    if (lowercaseHTML.Substring(i, CONTACTS_PAGE_SEARCH_KEYWORDS[j, k].Length).Equals(CONTACTS_PAGE_SEARCH_KEYWORDS[j, k]))
                                    {
                                        //This is where the various contact types are looked for in different places around each keyword
                                        String contact = "No " + CONTACTS_PAGE_SEARCH_KEYWORDS[j, 0].ToUpper() + " found";

                                        //maybe modify contact text in case extra text further is included in parsing
                                        //*********************************************************************************************************                   CONTINUE HERE (SEE BELOW STATEMENT FOR FURTHER GUIDANCE)
                                        //needs to find ALL possible contact information, not just one, then decide which one is the most likely contact info.
                                        contact = parseContactWithKeywordLocation(html, CONTACTS_PAGE_SEARCH_KEYWORDS[j, k], i, j);
                                        contactInfo[URLIndex, j] = contact;

                                        Console.WriteLine("Found " + CONTACTS_PAGE_SEARCH_KEYWORDS[j, 0].ToUpper() + " contact phrase in HTML at character #" + i);
                                        Console.WriteLine("Contact phrase: '" + contact + "'");
                                        
                                        //add the length of the search keyword found to i so it can rule that phrase out
                                        i += CONTACTS_PAGE_SEARCH_KEYWORDS[j, k].Length;
                                    }
                                }
                            }

                            //move on to the next HTML once it's finished reading through this HTML (up to the point of the last place to read the contact keywords from)
                            if (html.Substring(i + MAX_CONTACT_KEYWORD_LENGTH, 7) == ("</body>"))
                            {
                                endOfBody = true;
                                break;
                            }

                            i++;
                        }

                        Console.WriteLine("");
                    }
                    else
                    {
                        //set contact info of the contact to show that there is no contact info for it
                        //email
                        contactInfo[URLIndex, 0] = "There was no html at this URL";
                        //phone
                        contactInfo[URLIndex, 1] = "";
                        //other
                        contactInfo[URLIndex, 2] = "";

                        //debugging
                        Console.WriteLine("There was no html at this URL");
                    }
                }
                //if the url provided is empty
                else if (url != "Sorry, I couldn't find a contacts page.")
                {
                    //getting the url:
                    //check for a facebook link
                    contactURLs[URLIndex] = checkForFacebookLink(url);
                
                    //disabled for first rounds of testing
                    //if that fails, try brute-forcing the contact page url
                    //contactURLs[URLIndex] = tryBruteForce(url);

                    ////if that fails, try googling the entity based on the url stored in the respective URLs[] index
                    //contactURLs[URLIndex] = assembleGoogleURL();

                    //getting the contacts from the url or giving up
                    if (!(contactURLs[URLIndex] == "" || contactURLs[URLIndex].Length == 0))
                        getContactsFromURL(url);
                    else
                        contactURLs[URLIndex] = "Sorry, I couldn't find a contacts page.";
                }
                else
                {
                    //it should do this if no contacts page url is provided or found
                    //set contact info of the contact to show that there is no contact info for it
                    //email
                    contactInfo[URLIndex, 0] = "Somehow there was no html at this URL";
                    //phone
                    contactInfo[URLIndex, 1] = "Somehow there was no html at this URL";
                    //other
                    contactInfo[URLIndex, 2] = "Somehow there was no html at this URL";
                }

                //Reset endOfBody so that other methods can reuse the variable
                endOfBody = false;

            }
            catch (UriFormatException)
            {
                //if the URL is invalid, try googling the invalid url string maybe?
                Console.WriteLine("exception caused by url being '" + url + "'");
            }
}

        //Note: You can't break search for contacts from html SEGMENTS because contact info isn't virtually always contained in an "a" tag or a "button" tag like contact page urls are
        private static String parseItemFromList(String [] itemSearchKeywords, String[] parsingKeyWords, String [] listItems)
        {
            try {
                String item = "";
                int startIndex = 0;
                int endIndex = 0;
                String listItem = "";
                Boolean doBreakception = false;

                //loop through all listItems accumulated
                for (int i = 0; i < listItems.Length; i++)
                {
                    //listItems[i] should be a segment of HTML
                    listItem = listItems[i];

                    //fuck null values of listItems, all my homies use empty strings
                    if (listItem == null)
                        listItem = "";

                        for (int q = 0; q < itemSearchKeywords.Length; q++)
                        {

                            //if the searched phrase is within the listItem string (.ToLower to speed up search)
                            if (listItem.ToLower().LastIndexOf(itemSearchKeywords[q]) > 0)
                            {
                                //look for first listItem tag by going through detected html backwards
                                for (int j = listItem.Length - 5 - 1; j >= 0; j--)
                                {
                                    foreach (String parsingKeyWord in parsingKeyWords)
                                    {
                                        //"href=" and "src=" originally, lengths may need to be refactored further
                                        if (listItem.Substring(j, parsingKeyWord.Length) == parsingKeyWord)
                                        {
                                            //start reading the URL after the phrase above
                                            startIndex = j + parsingKeyWord.Length + 1;

                                            int k = 0;
                                            while (k < listItem.Length - startIndex - 1 && listItem[startIndex + k + 1] != null)
                                            {
                                                //break at the closing " in the html, signifying the end of the HTML
                                                //I'm leaving in the null test because I think the compiler gets mad at me when it's not there
                                                if (listItem[startIndex + k + 1] != null && listItem[startIndex + k + 1] == '"')
                                                    break;
                                                k++;
                                            }
                                            //if this is being written a ton of times, something is wrong
                                            Console.WriteLine("Found item " + item);
                                            item = listItem.Substring(startIndex, k + 1);
                                            doBreakception = true;
                                            break;
                                        }
                                    }
                                    if (doBreakception)
                                        break;
                                }
                            }
                            if (doBreakception)
                                break;
                        }
                        if (doBreakception)
                            break;
                }

                string tempItem = item;
                string baseURL = URLs[URLIndex];

                //check to make sure the url about to be returned is valid, and if it isn't make the returned url reflect that
                if (item.Length >= 4 && item.Substring(0, 4) != "http") {
                    //check all possible listItem extensions
                    for (int i = 0; i < URL_TYPE_EXTENSIONS.Length; i++)
                    {
                        //if the item ends with a URL_TYPE_EXTENSION from URL_TYPE_EXTENSIONS
                        if (item.Substring(item.Length - URL_TYPE_EXTENSIONS[i].Length, URL_TYPE_EXTENSIONS[i].Length) == URL_TYPE_EXTENSIONS[i]) {
                            tempItem = item;
                            break;
                        }
                        tempItem = "The items page url that I was going to return was not valid";
                    }

                    //check if the item url is a local listItem and convert it to a URL if it is, making sure to add a "/"
                    //if (item != "The items page url that I was going to return was not valid")
                    //    tempItem = baseURL.Substring(0, baseURL.LastIndexOf("")) + tempItem;
                    if (item.LastIndexOf("/") == -1)
                        return prepBaseURL(baseURL) + "/" + item;
                    else if (item.Substring(0, 1) == "/")
                        tempItem = prepBaseURL(baseURL);
                    else if (item.Substring(0, 2) == "./")
                        tempItem = prepBaseURL(baseURL) + item.Substring(1);
                }

                item = tempItem;

                return item;
            }
            catch (IndexOutOfRangeException)
            {
                return "listItem index went out of bounds on line 707";
            }
            catch (NullReferenceException)
            {
                return "listItem value at index was null on line 722";
            }
        }

        private static String prepBaseURL(String baseURL)
        {
            //if the baseURL doesn't have "/", or only has one at the end of "https://"
            if (baseURL.LastIndexOf("/") == -1 || baseURL.Substring(0, baseURL.LastIndexOf("/")+1).Equals("https://"))
                //return the original, unchanged baseURL
                return baseURL;


            String newBaseURL = baseURL.Substring(0, baseURL.LastIndexOf("/"));

            //otherwise, if the last bit on the url is actually a home page, remove that bit and return only the first part of the URL 
            foreach (String homePageKeyword in URL_REMOVE_EXTENSIONS)
                foreach (String extension in URL_REMOVE_EXTENSIONS)
                    //if the end of the baseURL is a homepage URL tidbit with the "/"
                    //the part where I did (newBaseURL.Length-1) was to get the last index of newBaseURL
                    if (newBaseURL.Substring((newBaseURL.Length-1) - (homePageKeyword + extension + 1).Length).Equals(homePageKeyword + extension + "/"))
                        //set the newBaseURL to the baseURL without that homepage bit
                        newBaseURL = newBaseURL.Substring(0, (newBaseURL.Length - 1) - (homePageKeyword + extension + 1).Length);
                    //no need to check for URLs that have the home page stuff withOUT the "/" at the end because they're gotten rid of near the top of this method, where newBaseURL is instantiated

            return newBaseURL;
        }

        //Note: following method gathers multiple starting and ending indices for potential contact information and returns the two corresponding with the shortest length contact information string (Yes I know this only works for short segments of contact info. but it should work well enough to get emails)
        //contactTypeIndex is an integer from 0-3 that indicates the type of contact info being searched for
        private static String parseContactWithKeywordLocation(String html, String keyword, int keywordIndexInHTML, int contactTypeIndex )
        {
            try
            {
                //reset the class variable so no old values carry over
                string[] contactSegments = new string[10000];

                ////offset so that the contact info. text phrase starts on the right word/phrase
                //int offset = 4;
                //Lists of possible contact start and end indices (Lengths correspond to the number of possible indices)
                int[] contactIndicesStart = new int[NUMBER_OF_CONTACT_PARSING_KEYWORDS];
                for (int i = 0; i < contactIndicesStart.Length; i++)
                    contactIndicesStart[i] = keywordIndexInHTML;
                int[] contactIndicesEnd = new int[NUMBER_OF_CONTACT_PARSING_KEYWORDS];
                for (int i = 0; i < contactIndicesEnd.Length; i++)
                    contactIndicesEnd[i] = -1;

                //keyword location
                //A type 1 statistical error in this case would be less severe than a type 2, as we'd still get the contact info if it were unclear whether or not the keyword was in the contact info
                Boolean keywordAtStartOfContact = false;
                //determine if the keyword is at the start of the contact info, by determining if the character before the keyword is empty or not
                //(primitive, I know, but it should be good enough for now at least)
                //if the character before the keywordIndexInHTML is empty, we assert that the scraper is starting at the start of the contact info
                if (html[keywordIndexInHTML - 1].Equals(" "))
                    keywordAtStartOfContact = true;

                //go through all start parsing phrases to find the contact info start indices if the keyword is in the middle of the contact info (ex: example@domain.com)
                if (!keywordAtStartOfContact)
                    //go through all start parsing phrases to determine if the end of the contact info has been found
                    for (int i = 0; i < NUMBER_OF_CONTACT_PARSING_KEYWORDS; i++)
                    {
                        //go through all characters in html before the keyword where a keyword can still be searched
                        for (int j = keywordIndexInHTML - 1; j > keyword.Length; j--)
                        {
                            //any starting index exceptions
                            ////exception for keyword "$" [0, 0]
                            //if (keyword.Equals("$"))
                            //    //set the start index to the index of the keyword, since that's how it should be formatted (+0 so memory location definitions aren't changed)
                            //    contactStartIndex = keywordIndexInHTML + 0;

                            //if the string at index i is equal to one of the parsing phrases from list (.ToLower() to speed up search)
                            if (html.ToLower().Substring(j, CONTACT_PARSING_KEYWORDS_START[contactTypeIndex, i].Length).Equals(CONTACT_PARSING_KEYWORDS_START[contactTypeIndex, i]))
                            {
                                //set the end index to the index after the current parsing phrase
                                contactIndicesStart[i] = j;// + CONTACT_PARSING_KEYWORDS_START[contactTypeIndex, i].Length - 1;
                                break;
                            }
                        }
                    }

                //go through all end parsing phrases to find the contact info end indices
                for (int i = 0; i < CONTACT_PARSING_KEYWORDS_END.Length; i++)
                {
                    //go through all characters in html after the keyword
                    for (int j = keywordIndexInHTML + keyword.Length; j < html.Length - CONTACT_PARSING_KEYWORDS_END[contactTypeIndex, i].Length; j++)
                    {
                        //any ending index exceptions
                        ////exception for keyword "$" [0, 0]
                        //if (keyword.Equals("$"))
                        //    //set the start index to the index of the keyword, since that's how it should be formatted (+0 so memory location definitions aren't changed)
                        //    contactStartIndex = keywordIndexInHTML + 0;

                        //if the phrase at index i is equal to one of the parsing phrases from list (.ToLower() to speed up search)
                        if (html.ToLower().Substring(j, CONTACT_PARSING_KEYWORDS_END[contactTypeIndex, i].Length).Equals(CONTACT_PARSING_KEYWORDS_END[contactTypeIndex, i]))
                        {
                            //set the end index to the index at the current parsing phrase
                            contactIndicesEnd[i] = j + 0;
                            break;
                        }
                    }
                }

                //get the largest valid start index and smallest valid end index around the contact information
                int startIndex = -1;
                foreach (int contactIndexStart in contactIndicesStart)
                    //if the start of the contact information is at the index where or before where the keyword was found and is greater than the previous startIndex value, set it as the new startIndex
                    if (contactIndexStart <= keywordIndexInHTML && contactIndexStart > startIndex)
                        startIndex = contactIndexStart + 0;
                int endIndex = html.Length;
                foreach (int contactIndexEnd in contactIndicesEnd)
                    //if the end of the contact information is after the startIndex and is less than the previous startIndex value, set it as the new endIndex
                    if (contactIndexEnd > startIndex && contactIndexEnd < endIndex)
                        endIndex = contactIndexEnd + 0;

                //add 1 to exclode
                startIndex++;

                //check if the contact info to be returned is less than a minimum size, and if it is, the text contained in the next HTML tag should be added
                if (html.Substring(startIndex, endIndex - startIndex).Length < 9)
                    return "The information I was going to return was too short to be real";

                //return the contact info
                //Note: there was an issue where the previous code would get a good string, but for whatever reason contactIndexEnd < contactIndexStart
                //&& contactIndexEnd > contactIndexStart
                //if (contactIndexEnd > 0 )
                //{
                Console.WriteLine("Character at contactIndexStart (" + startIndex + "): " + html[startIndex]);
                Console.WriteLine("Character at contactIndexEnd (" + endIndex + "): " + html[endIndex]);
                Console.WriteLine("");
                return html.Substring(startIndex, endIndex - startIndex);
                //}

                ////if it couldn't find an end index
                //return "Couldn't parse the text after the contact information keyword.";
            } catch (IndexOutOfRangeException)
            {
                Console.WriteLine("IndexOutOfRangeException for the CONTACT_PARSING_KEYWORDS_END array in line 1057");
                    return "IndexOutOfRangeException for the CONTACT_PARSING_KEYWORDS_END array in line 1057";
            }
        }

        private static String checkForFacebookLink(String url)
        {
            String[] searchKeywords = { "https://www.facebook.com/" };
            string html = getHTML(url);
            string foundURL = parseItemFromList(LINK_SEARCH_KEYWORDS, CONTACT_URL_PARSING_KEYWORDS, getURLsFromHTML(html, searchKeywords));

            return foundURL;
        }

        private static String tryBruteForce(String url, int stopAtMainExtensionIndex)
        {
            //find contacts page url given the main page url
            string baseURL = url;
            string foundURL = url;

            //check for/remove any removable phrases in the url
            for (int i = 0; i < URL_REMOVE_EXTENSIONS.Length; i++)
            {
                //if the last bit of the home page URL is a known removable phrase
                if (baseURL.Length - URL_REMOVE_EXTENSIONS[i].Length >= 0 && baseURL.Substring(baseURL.Length - URL_REMOVE_EXTENSIONS[i].Length) == URL_REMOVE_EXTENSIONS[i])
                {
                    //for each removable extension, reset the baseURL and set the foundURL to the baseURL before the removable phrase
                    baseURL = baseURL.Substring(0, baseURL.Length - URL_REMOVE_EXTENSIONS[i].Length);
                    foundURL = baseURL.Substring(0, baseURL.Length - URL_REMOVE_EXTENSIONS[i].Length);
                    //break statement just to speed things up and quit if/when a removable extension is found
                    break;
                }
            }

            //brute force all possible contact urls until one works
            for (int i = 0; i < URL_PRE_EXTENSIONS.Length; i++)
            {
                for (int j = 0; j < URL_MAIN_EXTENSION.Length; j++)
                {
                    for (int k = 0; k < URL_EXTENSION1.Length; k++)
                    {
                        for (int l = 0; l < URL_EXTENSION2.Length; l++)
                        {
                            for (int m = 0; m < URL_EXTENSION3.Length; m++)
                            {
                                for (int n = 0; n < URL_TYPE_EXTENSIONS.Length; n++)
                                {
                                    //if NOT(one of the intermediate string from the URL_EXTENSION1 list is currently chosen and the string from the URL_EXTENSION2 list is empty) DO this url check
                                    if (!(k > 0 && l == 0))
                                    {
                                        foundURL = prepBaseURL(baseURL) + URL_PRE_EXTENSIONS[i] + URL_MAIN_EXTENSION[j] + URL_EXTENSION1[k] + URL_EXTENSION2[l] + URL_EXTENSION3[m] + URL_TYPE_EXTENSIONS[n];
                                        //if the stopping location is hit and no correct contact url is found
                                        if (j == stopAtMainExtensionIndex)
                                            return "";
                                        //if the attempted URL is correct
                                        if (!(getHTML(foundURL) == ""))
                                            return foundURL;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //if a correct contact url is never found
            return "";
        }

        private static String checkFacebook()
        {
            //get contact's name
            WorkBook workbook = WorkBook.Load(PATH_OF_IO_DOC);
            WorkSheet worksheet = workbook.GetWorkSheet(SHEET_NAME);

            //convert the name of the contact to a string, replace all spaces with the query's special characters, then append the result to the base url
            String queryURL = "https://www.facebook.com/search/top?q="+ worksheet.Rows[URLIndex].Columns[NAMES_COLUMN].ToString().Replace(" ", "%20");



            //then look for the first search result's URL
            String[] searchKeywords = { };
            string html = getHTML(queryURL).Substring(0, getHTML(queryURL).Length - 0); //Get the HTML starting at where the search results should be
            string foundURL = parseItemFromList(LINK_SEARCH_KEYWORDS, CONTACT_URL_PARSING_KEYWORDS, getURLsFromHTML(html, searchKeywords));

            return foundURL;
        }

        private String assembleGoogleURL(Boolean assembleFromDocument)
        {
            if (assembleFromDocument)
            {
                //get contact's name
                WorkBook workbook = WorkBook.Load(PATH_OF_IO_DOC);
                WorkSheet worksheet = workbook.GetWorkSheet(SHEET_NAME);

                //convert the name of the contact to a string, replace all spaces with the query's special characters, then append the result to the base url
                return "https://www.google.com/search?q=" + worksheet.Rows[URLIndex].Columns[NAMES_COLUMN].ToString().Replace(",", "+");
            }
            else
                //convert the name of the contact to a string, replace all spaces with the query's special characters, then append the result to the base url
                return "https://www.google.com/search?q=" + KeywordsToScrape.Text.Replace(",", "+");
        }

        //                                                                               End of contact scraping section, beginning of contact info writing section
        //*************************************************************************************************************************************************************************

        private void buttonWriteContacts_Click(object sender, EventArgs e)
        {
            //read the URLs from the excel doc to an array of strings
            WorkBook workbook = WorkBook.Load(PATH_OF_IO_DOC);
            WorkSheet worksheet = workbook.GetWorkSheet(SHEET_NAME);

            int rowCount = NUMBER_OF_ENTRIES + rowOffset;
            int offset = rowOffset + 1;
            //start at rowOffset + 1 to skip the first header
            for (int i = offset; i < rowCount; i++)
            {
                //check to make sure correct values for correct column are written
                Console.WriteLine(i - offset);

                //set value by cell address
                //set value by row and column indexing
                worksheet[MAIN_URL_WRITING_COLUMN + i].Value = URLs[i - offset];
                Console.WriteLine(URLs[i - offset]);
                worksheet[CONTACT_URL_WRITING_COLUMN + i].Value = contactURLs[i - offset];
                Console.WriteLine(contactURLs[i - offset]);
                //only write information if it's selected in the GUI
                if (checkBoxIsChecked[0])
                {
                    worksheet[EMAIL_WRITING_COLUMN + i].Value = contactInfo[i - offset, 0];
                    Console.WriteLine(contactInfo[i - offset, 0]);
                }
                else
                {
                    Console.WriteLine("No email was written to the workbook.");
                }
                if (checkBoxIsChecked[1])
                {
                    worksheet[PHONE_WRITING_COLUMN + i].Value = contactInfo[i - offset, 1];
                    Console.WriteLine(contactInfo[i - offset, 1]);
                }
                else
                {
                    Console.WriteLine("No phone number was written to the workbook.");
                }
                if (checkBoxIsChecked[2])
                {
                    worksheet[ADDRESS_WRITING_COLUMN + i].Value = contactInfo[i - offset, 2];
                    Console.WriteLine(contactInfo[i - offset, 2]);
                }
                else
                {
                    Console.WriteLine("No address was written to the workbook.");
                }
                if (checkBoxIsChecked[3])
                {
                    worksheet[OTHER_WRITING_COLUMN + i].Value = contactInfo[i - offset, 2];
                    Console.WriteLine(contactInfo[i - offset, 2]);
                }
                else
                {
                    Console.WriteLine("No additional information was written to the workbook.");
                }

                Console.WriteLine("");
            }

            //save the altered workbook
            workbook.Save();
            Console.WriteLine("Finished writing contact information to workbook.");
        }

        //End of code

        #endregion

        //Instantiate the GUI elements
        private System.Windows.Forms.TabControl pageControl;
        private System.Windows.Forms.TabPage pageMain;
        private Label label2;
        private Label label1;
        private TabControl tabControlGatherMethod;
        private TabPage tabPageContactInfo;
        private CheckBox checkBoxAddress;
        private CheckBox checkBoxEmail;
        private CheckBox checkBoxPhone;
        private CheckBox checkBoxOther;
        private Button buttonLocateContacts;
        private Button buttonReadSites;
        private TabPage tabPageScraping;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private CheckBox checkBox4;
        private CheckBox checkBox3;
        private Button buttonScrapeURLs;
        private TabPage tabPageCustom;
        private RadioButton radioButtonBefore;
        private Label label7;
        private Label label6;
        private TextBox textBox4;
        private TextBox textBox3;
        private TextBox textBox2;
        private TextBox textBox1;
        private Label label5;
        private Label label4;
        private Label label3;
        private ListBox listBoxGatherMethod;
        private Button buttonWriteContacts;
        private Button buttonLoadURLs;
        private Label labelInfoToGather;
        private Label title1;
        private Button buttonCustomBack;
        private Button buttonCustomNext;
        private TabControl tabControlCustom;
        private TabPage tabPageKeywords;
        private TabPage tabPageBasicQuestions;
        private Label label9;
        private RadioButton radioButton1;
        private RadioButton radioButtonAfter;
        private RadioButton radioButton2;
        private TabPage tabPageCrawl;
        private Button button1;
        private Button button2;
        private Label label8;
        private TextBox textBox5;
        private TabPage tabPageScrape;
        private Button button3;
        private Button buttonScrapeGoogle;
        private Label label10;
        private TextBox KeywordsToScrape;
        private OpenFileDialog openFileDialog1;
    }
}

/*
Sources:
                                                https://stackoverflow.com/questions/16160676/read-excel-data-line-by-line-with-c-sharp-net
                                                https://www.wfmj.com/story/42504801/c-read-excel-file-example
                                                https://ironsoftware.com/csharp/excel/tutorials/csharp-open-write-excel-file/
    Prepping the Excel sheets (Add " " to the new cell values, use the =CONCAT function to concatenate all the urls into one cell, then use a word processor to replace the spaces with "", "", finally, copy+paste the string of urls into this program):
    Extracting URLs from hyperlinks (alt+f11):  https://excel.tips.net/T003281_Extracting_URLs_from_Hyperlinks.html
                                                http://zetcode.com/csharp/readwebpage/
                                                https://docs.microsoft.com/en-us/dotnet/api/system.string == ?view=net-5.0#System_String_StartsWith_System_String_
    An excel sheet from a friend to get some practice and testing data for the program
*/