using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(GameManager))]
public class Observer : MonoBehaviour
{
    [SerializeField] private PhysicsRaycaster _raycaster;
    [SerializeField] private float _delay;
    [SerializeField] private string _fileName;
    [SerializeField] private bool _needSerialize;
    [SerializeField] private bool _needDeserialize;
    [SerializeField] private List<string> _output;

    public event Action<int,int> NextStepReady;

    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager=GetComponent<GameManager>();
    }

    private void OnEnable()
    {
        _gameManager.StepOver += _gameManager_StepOver;
    }
    private void OnDisable()
    {
        _gameManager.StepOver -= _gameManager_StepOver;
    }

    private void _gameManager_StepOver()
    {
        if (!_needDeserialize && _needSerialize) return;
        if (string.IsNullOrEmpty(_output[0]))
        {
            _needDeserialize = false;
            _needSerialize = true;
            Debug.Log("Повтор закончен");
            _raycaster.enabled = true;
            return;
        }
        StartCoroutine(RepaetGame(_output[0]));
        _output.RemoveAt(0);
    }

    private void Start()
    {
        if (_needSerialize && !_needDeserialize)
        {
            File.Delete(_fileName+".txt");
        }

        if (_needDeserialize && !_needSerialize)
        {
            _raycaster.enabled = false;
            _output = Deserialize().Split(Environment.NewLine).ToList();
            _gameManager_StepOver();
        }
    }

    public async Task Serialize(string input)

    {
        if (!_needSerialize)
            return;

        await using var fileStream = new FileStream(_fileName + ".txt", FileMode.Append);
        await using var stredmWriter=new StreamWriter(fileStream);
        await stredmWriter.WriteLineAsync(input);
    }

    private string Deserialize()
    {
        if(!File.Exists(_fileName+".txt")) return null;


        using var fileStream = new FileStream(_fileName + ".txt", FileMode.Open);
        using var streamReader = new StreamReader(fileStream);

        var builder = new StringBuilder();

        while (!streamReader.EndOfStream)
        {
            builder.AppendLine(streamReader.ReadLine());
        }

        return builder.ToString();

    }

    private IEnumerator RepaetGame(string input)
    {
        string commandPatern = @"Player (\d+) (Move|Click|Remove)";
        string coordinatePatern = @"(\d+),(\d+)";

        yield return new WaitForSeconds(_delay);

        var commandMatch = Regex.Match(input, commandPatern);
        var indexPlayer = int.Parse(commandMatch.Groups[1].Value);
        var command = commandMatch.Groups[2].Value;

        var coordinateMathes = Regex.Matches(input, coordinatePatern);
        var originPosition = (
            int.Parse(coordinateMathes[0].Groups[1].Value),
            int.Parse(coordinateMathes[0].Groups[2].Value));
        switch (command)
        {
            case "Click":
                Debug.Log($"Player {indexPlayer} {command} to {originPosition}");
                NextStepReady?.Invoke(originPosition.Item1, originPosition.Item2);
                break;
            case "Move":
                var destinationPosition = (
           int.Parse(coordinateMathes[1].Groups[1].Value),
           int.Parse(coordinateMathes[1].Groups[2].Value));
                Debug.Log($"Player {indexPlayer} {command} from {originPosition} to {destinationPosition}");
                NextStepReady?.Invoke(destinationPosition.Item1, destinationPosition.Item2);
                break;
            case "Remove":
                Debug.Log($"Player {indexPlayer} {command} chip at {originPosition}");
                NextStepReady?.Invoke(-1, -1);
                break;
            default:
                throw new NullReferenceException("Action is null");
        }
    }
}
public enum RecordType
{
   Click, Move, Remove 
}