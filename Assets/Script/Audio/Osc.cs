using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Osc : MonoBehaviour
{
    public float FM = 44100f;
    int TM;

    [Range(20, 20000)]
    public float frecuencia = 100;

    public float TiempoSegundos = 2.0f;
    AudioSource audioSource;
    int timeIndex = 0;
    public Slider selector, level, Octava;
    public TextMeshProUGUI textoSeleccion, textonivel, textooctava;
    // variables para la selección de las formas de onda
    public enum WaveformType
    {
        Sine,
        Square,
        Sawtooth,
        Triangle
    }
    public WaveformType waveformType = WaveformType.Sine;

    // Arreglo de la wavetable
    public float[] wavetable;
    public int wavetableSize = 2048;

    // valores para la ADSR
    [Range(5, 200)]
    public float A = 100;
    [Range(10, 300)]
    public float D = 100;
    [Range(100, 5000)]
    public float S = 100;
    [Range(0.001f, 1f)]
    public float SLevel = 0.7f;
    [Range(10, 500)]
    public float R = 100;

    // ADSR, generador de envolvente sonora
    private float[] env;
    private int ADSRindex = 0;

    // Versión monofónica
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (env == null || wavetable == null) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            if (ADSRindex >= env.Length) break;

            float E = env[ADSRindex];
            float currentsample = 0f;

            try
            {
                currentsample += wavetable[(int)(phaseM * wavetableSize)];
                data[i] = currentsample * nivel;

                if (channels == 2)
                    data[i + 1] = currentsample * nivel;

                phaseM += frecuencia / FM;
                if (phaseM > 1f) phaseM -= 1f;

                ADSRindex++; // Movido aquí para actualizar solo cuando se procesa un sample
            }
            catch (System.IndexOutOfRangeException ex)
            {
                Debug.LogError("An IndexOutOfRangeException occurred in OnAudioFilterRead.");
                Debug.LogError("Error message: " + ex.Message);
                break;
            }
        }
    }

    float phaseM; // Se agrega la inicialización de phaseM

    // Start is called before the first frame update
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.Stop();
        GenerateWavetable();
        env = GetADSR();
        phaseM = 0f; // Se inicializa phaseM
    }

    // Update is called once per frame
    void Update()
    {
        TM = (int)(FM / frecuencia);
        funcion = (int)selector.value;
        textooctava.SetText(Octava.value.ToString());
    }

    // Funciones para la generación de cada forma de onda
    public int funcion = 0;
    public float CreateSeno(int timeIndex, float frecuencia)
    {
        return Mathf.Sin(2 * Mathf.PI * timeIndex * frecuencia / FM);
    }

    public float CreateSquare(int timeIndex, float frecuencia)
    {
        return Mathf.Sign(Mathf.Sin(2 * Mathf.PI * timeIndex * frecuencia / FM));
    }

    public float CreateTriangle(int timeIndex, float frecuencia)
    {
        float m1 = 1 / (TM / 4.0f);
        float m2 = -2 / ((TM * (3 / 4.0f) - (TM - 4.0f)));
        float m3 = 1 / (TM - (TM * (3 / 4.0f)));

        float b1 = 1 - (m1 * (TM / 4.0f));
        float b2 = 1 - (m2 * (TM / 4.0f));
        float b3 = 0 - (m3 * TM);

        if (timeIndex <= (TM / 4.0f)) return (m1 * timeIndex + b1);
        else if (timeIndex > (TM / 4.0f) && timeIndex <= (TM * (3 / 4f))) return (m2 * timeIndex + b2);
        else return (m3 * timeIndex + b3);
    }

    public float CreateSawTooth(int timeIndex, float frecuencia)
    {
        float m1 = 1 / (TM / 2f);
        float m2 = 1 / (TM - (TM / 2f));

        float b1 = 1 - (m1 * (TM / 2f));
        float b2 = 0 - (m2 * TM);

        if (timeIndex <= (TM / 2f)) return (m1 * timeIndex + b1);
        else return (m2 * timeIndex + b2);
    }

    // Arreglo de la wavetable
    private void GenerateWavetable()
    {
        wavetable = new float[wavetableSize];
        float f = FM / wavetableSize;
        for (int i = 0; i < wavetableSize; i++)
        {
            switch (waveformType)
            {
                case WaveformType.Sine:
                    wavetable[i] += CreateSeno(i, f);
                    break;
                case WaveformType.Square:
                    wavetable[i] += CreateSquare(i, f);
                    break;
                case WaveformType.Triangle:
                    wavetable[i] += CreateTriangle(i, f);
                    break;
                case WaveformType.Sawtooth:
                    wavetable[i] += CreateSawTooth(i, f);
                    break;
            }
        }
    }

    public void KeyboardDown(float f)
    {
        frecuencia = f * Mathf.Pow(2, Octava.value);
        if (!audioSource.isPlaying)
        {
            timeIndex = 0;
            audioSource.Play();
            funcion = 0;
            ADSRindex = 0;
        }
    }

    public void KeyboardUp()
    {
        frecuencia = 0;
        audioSource.Stop();
        timeIndex = 0;
        ADSRindex = 0;
    }

    // Resto de las funciones sin cambios...

    // Selección de forma de onda
    public void selection()
    {
        funcion = (int)selector.value;
        switch (funcion)
        {
            case 0:
                textoSeleccion.SetText("Sine Wave");
                break;
            case 1:
                textoSeleccion.SetText("Square wave");
                break;
            case 2:
                textoSeleccion.SetText("Triangle wave");
                break;
            case 3:
                textoSeleccion.SetText("Sawtooth wave");
                break;
        }
        GenerateWavetable();
    }

    // valor de amplitud calculado a partir de dBFS
    int Vref = 32768;
    float nivel = 1;

    public void amplitud()
    {
        nivel = DBFStoLinear(level.value);
        textonivel.SetText(Mathf.Round(level.value).ToString());
    }

    // Convierte de dBFS a lineal
    float DBFStoLinear(float dBfs)
    {
        return Mathf.Pow(10f, (dBfs / 20f));
    }

    // ADSR, generador de envolvente sonora
    float[] GetADSR()
    {
        int totalADSRSize = (int)(FM * (A + D + S + R));
        float[] envelope = new float[totalADSRSize];
        int ASamples = (int)(FM * A);
        int DSamples = (int)(FM * D);
        int SSamples = (int)(FM * S);
        int RSamples = (int)(FM * R);
        for (int i = 0; i < totalADSRSize; i++)
        {
            float value = 0f;

            if (i < ASamples) value = Mathf.Lerp(0f, 1f, (float)i / ASamples);
            else if (i < ASamples + DSamples) value = Mathf.Lerp(1f, SLevel, (float)i / DSamples);
            else if (i < ASamples + DSamples + SSamples) value = SLevel;
            else if (i < ASamples + DSamples + SSamples + RSamples) value = Mathf.Lerp(SLevel, 0f, (float)i / RSamples);

            envelope[i] = value;
        }
        return envelope;
    }

    [Range(0, 1)]
    public float[] Amplitudes = new float[10] { 1, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.4f, 0.4f, 0.4f };
    int armonicos = 10;
    // --------------------------------------------------------------------------------

    public void PlaySong()
    {
        StartCoroutine(Song());    
    }

    /* 
    - Entre menor sea el tempo, menor será el tiempo por nota
    - Multiplicar TimePerNote por el tiempo de cada nota
        Según clave de Sol:
        Redonda: *4
        Blanca: *2
        Negra: *1
        Corchea: *1/2
        Semicorchea: *1/4        
    */

    // Corutina: Cancion procedural
    IEnumerator Song()
    {
        float tempo = 160f;
        float TimePerNote = 60 / tempo;
        Octava.value = 0;
        waveformType = WaveformType.Square;

        // María tenía un corderito
        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(192.998f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote * 2);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote * 2);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote * 2);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(192.998f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote * 2);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(246.942f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(220f);
        yield return new WaitForSeconds(TimePerNote);
        KeyboardUp();

        KeyboardDown(192.998f);
        yield return new WaitForSeconds(TimePerNote * 4);
        KeyboardUp();
    }
}
