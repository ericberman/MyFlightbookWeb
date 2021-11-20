using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Airports
{
    /// <summary>
    /// A single question for an airport identification quiz.  Includes a set of answers and the index of the correct one
    /// </summary>
    [Serializable]
    public class AirportQuizQuestion
    {
        private int m_CorrectAnswer = -1;
        private readonly airport[] m_Answers;

        /// <summary>
        /// The index of the answer to the most recently asked question
        /// </summary>
        public int CorrectAnswerIndex
        {
            get { return m_CorrectAnswer; }
            set { m_CorrectAnswer = value; }
        }

        /// <summary>
        /// The airports that comprise the choices for the user; the correct one is in index CorrectAnswerIndex
        /// </summary>
        public ReadOnlyCollection<airport> Answers
        {
            get { return new ReadOnlyCollection<airport>(m_Answers); }
        }

        public AirportQuizQuestion(airport[] answers, int correctAnswerIndex)
        {
            m_Answers = answers;
            CorrectAnswerIndex = correctAnswerIndex;
        }
    }

    [Serializable]
    public class AirportQuiz
    {
        public const string szBusyUSAirports = "KABQ;KALB;KATL;KAUS;KBDL;KBHM;KBNA;KBOI;KBOS;KBTV;KBUF;KBUR;KBWI;KCAK;KCHS;KCLE;KCLT;KCMH;KCOS;KCVG;KDAL;KDAY;KDCA;KDEN;KDFW;KDSM;KDTW;KELP;KEWR;KFLL;KGEG;KGRR;KGSO;PHNL;KHOU;KHPN;KIAD;KIAH;KICT;KIND;KISP;KJAN;KJAX;KJFK;PHKO;KLAS;KLAX;KLGA;KLGB;PHLI;KLIT;KMCI;KMCO;KMDW;KMEM;KMHT;KMIA;KMKE;KMSN;KMSP;KMSY;KMYR;KOAK;PHOG;KOKC;KOMA;KONT;KORD;KORF;KPBI;KPDX;KPHL;KPHX;KPIT;KPNS;KPVD;KPWM;KRDU;KRIC;KRNO;KROC;KRSW;KSAN;KSAT;KSAV;KSDF;KSEA;KSFO;KSJC;TJSJ;KSLC;KSMF;KSNA;KSTL;KSYR;KTPA;KTUL;KTUS;KTYS";

        private int m_CurrentIndex = -1;
        private int m_CorrectAnswerCount = -1;
        private int[] m_rgShuffle;
        private airport[] m_rgAirport;
        private int m_cBluffs = 3;

        #region Properties
        /// <summary>
        /// The index of the current question
        /// </summary>
        public int CurrentQuestionIndex
        {
            get { return m_CurrentIndex; }
            set { m_CurrentIndex = value; }
        }

        /// <summary>
        /// The # of correct answers given so far
        /// </summary>
        public int CorrectAnswerCount
        {
            get { return m_CorrectAnswerCount; }
            set { m_CorrectAnswerCount = value; }
        }

        /// <summary>
        /// Returns the random object in use, so that it doesn't have to be recreated.
        /// </summary>
        public Random Random { get; set; }

        /// <summary>
        /// Gets/sets the # of bluff answers for each question
        /// </summary>
        public int BluffCount
        {
            get { return m_cBluffs; }
            set { m_cBluffs = value > 0 ? value : throw new MyFlightbookException("Bluffs must be > 0"); }
        }

        /// <summary>
        /// Returns the current question
        /// </summary>
        public AirportQuizQuestion CurrentQuestion { get; set; }
        #endregion

        /// <summary>
        /// Creates a new AirportQuiz object, which holds the state for an airport quiz
        /// </summary>
        /// <param name="szDefaultAirportList">The delimited list of airport codes to use for the quiz.  Defaults to szBusyUSAirports</param>
        public AirportQuiz()
        {
            this.Random = new Random((int)(DateTime.Now.Ticks % Int32.MaxValue));
            CorrectAnswerCount = 0;
        }

        /// <summary>
        /// Returns the list of airports used to initialize this quiz
        /// </summary>
        public IEnumerable<airport> DefaultAirportList { get; set; }

        /// <summary>
        /// Initializes a quiz, shuffling the airports and resetting the current quiz index and correct answer count.  You must then do GenerateQuestion to actually create a question.
        /// </summary>
        /// <param name="szDefaultAirportList"></param>
        public void Init(airport[] rgAirports)
        {
            DefaultAirportList = rgAirports ?? throw new ArgumentNullException(nameof(rgAirports));

            m_rgAirport = rgAirports;
            m_rgShuffle = ShuffleIndex(m_rgAirport.Length);
            CurrentQuestionIndex = 0;
            CurrentQuestion = null;
            m_CorrectAnswerCount = 0;
        }

        /// <summary>
        /// Generate a new question, increment the current question index
        /// </summary>
        /// <returns></returns>
        public void GenerateQuestion()
        {
            airport[] rgAirportAnswers = new airport[BluffCount + 1];
            CurrentQuestion = new AirportQuizQuestion(rgAirportAnswers, this.Random.Next(BluffCount + 1));

            // set the index of the correct answer
            rgAirportAnswers[CurrentQuestion.CorrectAnswerIndex] = m_rgAirport[m_rgShuffle[this.CurrentQuestionIndex]];

            // fill in the ringers...
            int[] rgBluffs = ShuffleIndex(m_rgAirport.Length);
            for (int i = 0, iBluff = 0; i < BluffCount; i++)
            {
                // see if the airport index of the bluff is the same as the index of the correct answer
                if (rgBluffs[iBluff] == m_rgShuffle[CurrentQuestionIndex])
                    iBluff = (iBluff + 1) % rgBluffs.Length;

                int iResponse = (CurrentQuestion.CorrectAnswerIndex + i + 1) % (BluffCount + 1);

                rgAirportAnswers[iResponse] = m_rgAirport[rgBluffs[iBluff]];

                iBluff = (iBluff + 1) % rgBluffs.Length;
            }

            m_CurrentIndex++;
        }

        /// <summary>
        /// Returns a shuffled array of indices from 0 to count-1
        /// </summary>
        /// <param name="count">Highest allowed value in the array</param>
        /// <param name="r">(optional) the random number generator to use</param>
        /// <returns>A shuffled array of integers</returns>
        private int[] ShuffleIndex(int count)
        {
            int[] rgShuffle = new int[count];

            for (int i = 0; i < count; i++)
            {
                rgShuffle[i] = i;
            }

            for (int i = 0; i < count; i++)
            {
                int k = this.Random.Next(i, count);
                int j = rgShuffle[i];
                rgShuffle[i] = rgShuffle[k];
                rgShuffle[k] = j;
            }

            return rgShuffle;
        }
    }
}