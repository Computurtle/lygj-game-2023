using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LYGJ.QuestSystem;

public class StartQuest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Quests.Start("harvest-festival");
    }
}
