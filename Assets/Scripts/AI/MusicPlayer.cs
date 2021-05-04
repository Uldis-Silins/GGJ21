﻿using System.Collections;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioSource menuMusic;
    public AudioSource gameMusic;

    public bool fadeOnStart;

    private float m_fadeTimer;
    private readonly float m_fadeTime = 2f;

    private GameState m_curGameState;
    private bool m_isFading;

    private static MusicPlayer m_instance;

    private void Awake()
    {
        if(m_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        m_instance = this;
    }

    private void Start()
    {
        if(!fadeOnStart)
        {
            m_fadeTimer = m_fadeTime;
            m_curGameState = Player_Controller.currentGameState;
        }
    }

    private void Update()
    {
        if (Player_Controller.currentGameState != m_curGameState)
        {
            m_curGameState = Player_Controller.currentGameState;
            m_fadeTimer = 0f;
            m_isFading = true;
        }

        if (m_isFading)
        {
            m_fadeTimer += Time.unscaledDeltaTime;

            if (m_curGameState == GameState.Playing)
            {
                gameMusic.volume = Mathf.Lerp(0f, 1f, m_fadeTimer / m_fadeTime);
                menuMusic.volume = Mathf.Lerp(1f, 0f, m_fadeTimer / m_fadeTime);
            }
            else
            {
                gameMusic.volume = Mathf.Lerp(1f, 0f, m_fadeTimer / m_fadeTime);
                menuMusic.volume = Mathf.Lerp(0f, 1f, m_fadeTimer / m_fadeTime);
            }

            if (m_fadeTimer >= m_fadeTime)
            {
                m_isFading = false;
            }
        }
    }
}