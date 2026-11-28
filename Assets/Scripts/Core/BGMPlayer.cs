using UnityEngine;

public class BGMPlayer : MonoBehaviour {
    public static BGMPlayer Instance;

    private AudioSource _audioSource;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;

        // 读取保存的音量
        _audioSource.volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
    }

    void Start() {
        // 自动播放保存的歌曲
        int savedChoice = PlayerPrefs.GetInt("MusicChoice", 0);
        string clipName = savedChoice switch {
            0 => "music/SubstituteForSunrise",
            1 => "music/Freeze",
            2 => "music/infinity",
            _ => "music/SubstituteForSunrise"
        };
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip != null) {
            PlayClip(clip);
        }
    }

    public void PlayClip(AudioClip clip) {
        if (clip == null) return;
        if (_audioSource.clip == clip && _audioSource.isPlaying) return;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    public void SetVolume(float v) {
        _audioSource.volume = v;
    }
}