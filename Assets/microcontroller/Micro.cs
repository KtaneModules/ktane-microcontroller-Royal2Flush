using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.IO;

public class Micro : MonoBehaviour {

    //----------------
    // initialization
    //----------------

    // Unity objects

	public KMSelectable buttonOK;
	public KMSelectable buttonDown;
	public KMSelectable buttonUp;
    public buttonAnim _buttonAnim;
    public buttonAnim _buttonDownAnim;
    public buttonAnim _buttonUpAnim;

    public GameObject[] LEDS;
    public Material[] LEDMaterials; //order: black, white, red, yellow, purple, blue, green

    public GameObject MicBig;
    public GameObject MicMed;
    public GameObject MicSmall;
    public TextMesh MicSerial;
    public TextMesh MicType;
    public GameObject Dot;

    public GameObject Background;
    public Material BG5;
    public Material BG4;
    public Material BG3;
    //public TextMesh DebugText;

    public Color TextColorDark;
    public Color TextColorLight;

    // global game logic elements

	private int controllerNr;

    private int currentLEDIndex = 0;
    private int materialID = 0;


    private List<int> LEDorder = new List<int>(); // orde rin which the LEDs activate
	private int[] solutionRaw; // 0: GND, 1-5: VCC, AIN, DIN, PWM, RST
	private int[] colorMap; // index order: see solutionRaw; value order: see LEDMaterials
	private int[] positionTranslate; // which number in the datasheet is which index of the LEDs

    private int solved;



    // Use this for initialization
    void Start()
    {
        //GetComponent<KMGameInfo>().OnLightsChange += OnLightsChange; // this calls the function OnLightsChange whenever the lights in the game go on or off
        Init();
    }

    void Init()
    {
        Debug.Log("fnxMicro: started Init");
        solved = 0;

        //---------------------------------------------
        // set all LEDS to black material
        //---------------------------------------------

        foreach (GameObject led in LEDS)
        {
            led.transform.Find("Plane.001").gameObject.GetComponent<Renderer>().material = LEDMaterials[0];
        }

        //------------------------------------------------------------
        // determine the correct distances for translation of the dot
        //------------------------------------------------------------

        // these values are unitless and just relative to each other
        float transZ = 0.04f; // that's up/down
        float transX6 = -0.03f; // that's right/left
        float transX8 = -0.048f;
        float transX10 = -0.065f;
        // and now scale it to what it really is (important with mods that change the size of mudules i.e. double decker)
        Vector3 Scale = MicBig.transform.lossyScale / 18.3f;
        transZ = transZ * Scale.z;
        transX6 = transX6 * Scale.x;
        transX8 = transX8 * Scale.x;
        transX10 = transX10 * Scale.x;

        //-----------------------
        // roll the module specs
        //-----------------------

        int dotPos = UnityEngine.Random.Range(0, 4);
        int micnum = UnityEngine.Random.Range(0, 12);
        //DebugText.text = "DP " + dotPos.ToString() + " MCN " + micnum.ToString();

        //------------------------------------------
        // prepare module according to roll, Part 1
        //------------------------------------------

        // this includes the following steps (here in that order):
        // set correct background
        // show the correct model
        // hide not used LEDs
        // move the dot according to what we rolled
        // set the pin numbering
        // setup the order in which the LEDs will activate
        //
        // explanation on pin numbering:
        // the numbering the players use (i.e. in the manual) starts in one corner and then goes around in a circle.
        // the internal numbering is {top left, bottom left, top 2. from left, bottom 2. from left, ...}
        // the array positionTranslate translates between these two systems.
        //
        // oh, and if you're wondering why the lowest value in positionTranslate is 1 and not 0:
        // I fucked up and was too lazy to change it manually. I just fix it after the if-else-block ;-)

        if (micnum == 0 || micnum==1 || micnum==2 || micnum==3) // 6 pins
        {

            Background.GetComponent<Renderer>().material = BG3;

            MicBig.GetComponent<Renderer>().enabled = false;
            MicMed.GetComponent<Renderer>().enabled = false;
            MicSmall.GetComponent<Renderer>().enabled = true;

            LEDS[6].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[6].transform.Find("Plane").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[7].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[7].transform.Find("Plane").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[8].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[8].transform.Find("Plane").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[9].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[9].transform.Find("Plane").gameObject.GetComponent<Renderer>().enabled = false;

            switch (dotPos)
            {
                case 0: // dot top left

                    Dot.transform.Translate(0, 0, 0, Space.Self);
					positionTranslate = new int[6] {1,6,2,5,3,4};

                    break;

                case 1: // dot top right

                    Dot.transform.Translate(transX6, 0, 0, Space.Self);
					positionTranslate = new int[6] {3,4,2,5,1,6};

                    break;

                case 2: // dot bottlom left

                    Dot.transform.Translate(0, 0, transZ, Space.Self);
					positionTranslate = new int[6] {6,1,5,2,4,3};

                    break;

                case 3: // dot bottom right

                    Dot.transform.Translate(transX6, 0, transZ, Space.Self);
					positionTranslate = new int[6] {4,3,5,2,6,1};

                    break;

            }

            RandomizeLEDOrder(6);
        }
        else if (micnum == 4 || micnum == 5 || micnum== 6 || micnum == 7) // 8 pins
        {
            Background.GetComponent<Renderer>().material = BG4;

            MicBig.GetComponent<Renderer>().enabled = false;
            MicMed.GetComponent<Renderer>().enabled = true;
            MicSmall.GetComponent<Renderer>().enabled = false;

            LEDS[8].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[8].transform.Find("Plane").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[9].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().enabled = false;
            LEDS[9].transform.Find("Plane").gameObject.GetComponent<Renderer>().enabled = false;

            switch (dotPos) // for explanations on the numbers see 6 pins
            {
                case 0:

                    Dot.transform.Translate(0, 0, 0, Space.Self);
					positionTranslate = new int[8] {1,8,2,7,3,6,4,5};

                    break;

                case 1:

                    Dot.transform.Translate(transX8, 0, 0, Space.Self);
					positionTranslate = new int[8] {4,5,3,6,2,7,1,8};

                    break;

                case 2:

                    Dot.transform.Translate(0, 0, transZ, Space.Self);
					positionTranslate = new int[8] {8,1,7,2,6,3,5,4};

                    break;

                case 3:

                    Dot.transform.Translate(transX8, 0, transZ, Space.Self);
					positionTranslate = new int[8] {5,4,6,3,7,2,8,1};

                    break;

            }

            RandomizeLEDOrder(8);
        }
        else // 10 pins
        {

            Background.GetComponent<Renderer>().material = BG5;

            MicBig.GetComponent<Renderer>().enabled = true;
            MicMed.GetComponent<Renderer>().enabled = false;
            MicSmall.GetComponent<Renderer>().enabled = false;

            switch (dotPos) // for explanations on the numbers see 6 pins
            {


                case 0:

                    Dot.transform.Translate(0, 0, 0, Space.Self);
					positionTranslate = new int[10] {1,10,2,9,3,8,4,7,5,6};

                    break;

                case 1:

                    Dot.transform.Translate(transX10, 0, 0, Space.Self);
					positionTranslate = new int[10] {5,6,4,7,3,8,2,9,1,10};

                    break;

                case 2:

                    Dot.transform.Translate(0, 0, transZ, Space.Self);
					positionTranslate = new int[10] {10,1,9,2,8,3,7,4,6,5};

                    break;

                case 3:

                    Dot.transform.Translate(transX10, 0, transZ, Space.Self);
					positionTranslate = new int[10] {6,5,7,4,8,3,9,2,10,1};

                    break;

            }

            RandomizeLEDOrder(10);
        }

        for (int i = 0; i < positionTranslate.Length; i++)
        {
            positionTranslate[i] -= 1; // that's because I wrote positionTranslate down starting with 1 when it should start with 0
        }


        //------------------------------------------
        // prepare module according to roll, Part 2
        //------------------------------------------

        // this includes the following steps (here in that order):
        // set the text for the type and the serial number
        // set the solution
        // light the first LED
        //
        // it's calles "solutionRaw" because that's what's how it is in the manual. To work with it we'll go through several bookkeeping arrays.
        // Because that helps making it more comprehensible... /s

        switch (micnum)
        {
            case 0:

                MicType.text = "STRK";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(100, 1000) + "-" + UnityEngine.Random.Range(0, 10);

				solutionRaw = new int[6] {2,1,5,3,4,0};
                break;

            case 1:

                MicType.text = "LEDS";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(100, 1000) + "-" + UnityEngine.Random.Range(0, 10);

				solutionRaw = new int[6] {4,5,1,3,2,0};
				
                break;

            case 2:
                
                MicType.text = "CNTD";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(100, 1000) + "-" + UnityEngine.Random.Range(0, 10);

				solutionRaw = new int[6] {0,2,4,1,3,5};

                break;

            case 3:
                
                MicType.text = "EXPL";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(100, 1000) + "-" + UnityEngine.Random.Range(0, 10);

				solutionRaw = new int[6] {4,1,5,2,3,0};
  
                break;

            case 4:
                
                MicType.text = "STRK";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(1000, 10000) + "-" + UnityEngine.Random.Range(10, 100);

				solutionRaw = new int[8] {2,4,0,3,1,0,5,0};

                break;

            case 5:
                
                MicType.text = "LEDS";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(1000, 10000) + "-" + UnityEngine.Random.Range(10, 100);

				solutionRaw = new int[8] {4,3,1,0,2,0,5,0};

                break;

            case 6:
                
                MicType.text = "CNTD";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(1000, 10000) + "-" + UnityEngine.Random.Range(10, 100);

				solutionRaw = new int[8] {4,0,0,1,2,0,3,5};

                break;

            case 7:
                
                MicType.text = "EXPL";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(1000, 10000) + "-" + UnityEngine.Random.Range(10, 100);

				solutionRaw = new int[8] {2,0,5,0,1,0,3,4};

                break;

            case 8:
                
                MicType.text = "STRK";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(10000, 100000) + "-" + UnityEngine.Random.Range(100, 1000);

				solutionRaw = new int[10] {0,0,0,0,2,3,0,1,5,4};

                break;

            case 9:
                
                MicType.text = "LEDS";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(10000, 100000) + "-" + UnityEngine.Random.Range(100, 1000);

				solutionRaw = new int[10] {4,2,3,0,0,0,0,5,1,0};

                break;

            case 10:
                
                MicType.text = "CNTD";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(10000, 100000) + "-" + UnityEngine.Random.Range(100, 1000);

				solutionRaw = new int[10] {4,3,2,0,0,1,0,0,5,0};

                break;

            case 11:
                
                MicType.text = "EXPL";
                MicSerial.text = "FNX " + UnityEngine.Random.Range(10000, 100000) + "-" + UnityEngine.Random.Range(100, 1000);

				solutionRaw = new int[10] {5,3,1,0,0,0,2,0,4,0};

                break;

        }

        LEDS[LEDorder[currentLEDIndex]].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().material = LEDMaterials[1];
        materialID = 1;


        GetComponent<KMBombModule>().OnActivate += OnActivate;

        //Debug.Log("qwertz---ended Init");
        
    }


    void OnActivate()
    {
        //Debug.Log("qwertz---started onActivate");

        //I guess that's some failed attempt at doing ... something? I don't remember, but I'll leave it here. Just ignore it...

        /*StreamWriter sw = new StreamWriter("ktane.txt");
        foreach (string query in new List<string> { KMBombInfo.QUERYKEY_GET_BATTERIES, KMBombInfo.QUERYKEY_GET_INDICATOR, KMBombInfo.QUERYKEY_GET_PORTS, KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER })
        {
            List<string> queryResponse = GetComponent<KMBombInfo>().QueryWidgets(query, null);

            if (queryResponse.Count > 0)
            {
                sw.WriteLine(queryResponse[0]);
                Debug.Log(queryResponse[0]);
            }
        }

        sw.Close();*/


        //------------------------------------------------
        // fetch indicators, batteries, etc from the bomb
        //------------------------------------------------

        // indicators

        List<string> indicatorOn = new List<string>();
        List<string> indicatorOff = new List<string>();
 
        List<string> responsesIND = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_INDICATOR, null);
        foreach (string response in responsesIND)
        {
            Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            if (responseDict["on"] == "True")
            {
                indicatorOn.Add(responseDict["label"]);
            }
            else
            {
                indicatorOff.Add(responseDict["label"]);
            }
           
        }


        // ports

        bool hasRJ45 =  false;
        List<string> ports = new List<string>();

        List<string> responsesPorts = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null);
        foreach (string response in responsesPorts)
        {
            Dictionary<string, List<string>> responseDict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(response);

            ports = responseDict["presentPorts"];

            if (ports.Contains("RJ45"))
            {
                hasRJ45 = true;
            }

        }


        // serial number

        string serial = "";

        List<string> responsesSerial = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
        foreach (string response in responsesSerial)
        {
            Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            serial = responseDict["serial"];

        }


        // batteries
        
        int batteryCount = 0;

        List<string> responsesBat = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_BATTERIES, null);
        foreach (string response in responsesBat)
        {
            Dictionary<string, int> responseDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(response);
            batteryCount += responseDict["numbatteries"];
        }
		

        //--------------------------------
        // set the color map for the LEDs
        //--------------------------------
		
		if (MicSerial.text[MicSerial.text.Length-1] == '1' || MicSerial.text[MicSerial.text.Length-1] == '4')
		{
			//MicType.text += "-1"; // that one was for ingame debugging. I'll leave it here just in case there still is some bug
			colorMap = new int[6] {1,3,4,6,5,2};
		}
        else if (indicatorOn.Exists(i => i == "SIG") || hasRJ45)
		{
			//MicType.text += "-2";
			colorMap = new int[6] {1,3,2,4,6,5};
		}
		else if (serial.Contains("C") || serial.Contains("L") || serial.Contains("R") || serial.Contains("X") || serial.Contains("1") || serial.Contains("8"))
		{
			//MicType.text += "-3";
			colorMap = new int[6] {1,2,4,6,5,3};
		}
		else if (MicSerial.text[5].ToString() == batteryCount.ToString())
		{
			//MicType.text += "-4";
			colorMap = new int[6] {1,2,5,3,6,4};
		}
		else
		{
			//MicType.text += "-5";
			colorMap = new int[6] {1,6,2,3,5,4};
		}


        //-----------------
        // prepare buttons
        //-----------------

        buttonOK.OnInteract += delegate () { PressedOK(); return false; };
        buttonDown.OnInteract += delegate () { CycleDown(); return false; };
        buttonUp.OnInteract += delegate () { CycleUp(); return false; };

        //Debug.Log("qwertz---ended onActivate");
    }

    private void OnLightsChange(bool ison) // this nifty little function sets the text color according to the light state, so the text does not appear glowing in the dark
    {
        if (ison)
        {
            MicSerial.color = TextColorLight;
            MicType.color = TextColorLight;
        }
        else
        {
            MicSerial.color = TextColorDark;
            MicType.color = TextColorDark;
        }
    }

    void PressedOK()
    {
        // we pressed ok. Exciting times!

        // is the module already solved? If yes, we do nothing.
        if (solved == 0)
        {
            // audiovisual feedback on button press
            _buttonAnim.press();
            GetComponent<KMAudio>().HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

            // is the solution correct?
            if (materialID == colorMap[solutionRaw[positionTranslate[LEDorder[currentLEDIndex]]]]) // <-- I told you: Lots of bookkeeping :-D
            {
                // are we done with the module?
                if (currentLEDIndex == LEDorder.Count - 1)
                {
                    // yes -> shut it down
                    GetComponent<KMBombModule>().HandlePass();
                    solved = 1;
                }
                else
                {
                    // no -> onto the next LED!
                    currentLEDIndex++;

                    // turn the next LED on
                    materialID = 1;
                    LEDS[LEDorder[currentLEDIndex]].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().material = LEDMaterials[materialID];
                }
            }
            else // solution incorrect
            {
                GetComponent<KMBombModule>().HandleStrike();
                // In case we get a strike when we shouldn't: Ingame debugging:
                //DebugText.text = "";
                //DebugText.text += (" LED " + LEDorder[currentLEDIndex].ToString() + "\n");
                //DebugText.text += ("Pin " + positionTranslate[LEDorder[currentLEDIndex]].ToString() + "\n");
                //DebugText.text += ("P " + solutionRaw[positionTranslate[LEDorder[currentLEDIndex]]].ToString() + " C " + colorMap[solutionRaw[positionTranslate[LEDorder[currentLEDIndex]]]].ToString());
            }
        }
    }

    void CycleUp()
    {
        // we want to change the LED color in a certain direction

        // is the module already solved? If yes, we do nothing.
        if (solved == 0)
        {
            // audiovisual feedback on button press
            _buttonUpAnim.press();
            GetComponent<KMAudio>().HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

            // cycle through the colors ...
            if (materialID < 6)
            {
                materialID++;
            }
            else
            {
                materialID = 1;
            }

            // ... then set the LED to the new color
            LEDS[LEDorder[currentLEDIndex]].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().material = LEDMaterials[materialID];
        }
    }


    void CycleDown()
    {
        // we want to change the LED in a certain direction
        // for comments see CycleDown()
        if (solved == 0)
        {

            _buttonDownAnim.press();
            GetComponent<KMAudio>().HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

            if (materialID > 1)
            {
                materialID--;
            }
            else
            {
                materialID = 6;
            }

            LEDS[LEDorder[currentLEDIndex]].transform.Find("Plane.001").gameObject.GetComponent<Renderer>().material = LEDMaterials[materialID];
        }
    }

    void RandomizeLEDOrder(int Pins)
    {
        // Sets the order in which the LEDs will light up.
        // It's just a quick and dirty (and at runtime probably horribly inefficient) list randomizer.

        int temp = 0;
        int doit = 0;

        while (LEDorder.Count != Pins)
        {
            temp = UnityEngine.Random.Range(0, Pins);
            doit = 0;

            foreach (int j in LEDorder)
            {
                if (j == temp)
                {
                    doit = 1;
                }
            }

            if (doit == 0)
            {
                LEDorder.Add(temp);
            }   
        }
    }
}
