using System;
using Unity.InteractiveTutorials;
using UnityEngine;

public class Readme : ScriptableObject
{
    public Texture2D icon;
    public string title;
    public string description;
    public bool loadedLayout;

    [Space(10)]
    [Header("Colors")]
    public Color TextColorMainDarkSkin = Color.white;
    public Color TextColorMainLightSkin = Color.black;

    public Color TextColorSecondaryDarkSkin = new Color(0.66f,0.66f,0.66f);
    public Color TextColorSecondaryLightSkin = new Color(0.33f,0.33f,0.33f);

    [Space(10)]
    [Header("Text Sizes")]
    public int TextTitleSize = 24;
    public int TextProjectDescriptionSize = 26;
    public int TextHeadingSize = 18;
    public int TextBodySize = 14;
    public int ButtonTextSize = 20;

    public Section[] sections;

    [Serializable]
    public class Section
    {
        public int orderInView;
        public string heading, text, linkText, url, buttonText;
        public bool CanDrawButton
        {
            get
            {
                return (!string.IsNullOrEmpty(buttonText) && tutorial);
            }
        }

        [SerializeField]
        Tutorial tutorial = null;

        public void StartTutorial()
        {
            TutorialManager.instance.StartTutorial(tutorial);
        }
    }

    void OnValidate()
    {
        SortSections();
        for (int i = 0; i < sections.Length; ++i)
        {
            sections[i].orderInView = i * 2;
        }
    }

    void SortSections()
    {
        Array.Sort(sections, (x, y) => x.orderInView.CompareTo(y.orderInView));
    }
}
