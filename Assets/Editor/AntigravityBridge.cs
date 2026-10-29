#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Antigravity Live Editor Bridge for Unity 6.
/// Enables AI-assisted development loops: immediate compilation refresh,
/// real-time status checks, and console log inspection.
/// </summary>
[InitializeOnLoad]
public static class AntigravityBridge
{
    private const int Port = 8089;
    private static HttpListener _listener;
    private static CancellationTokenSource _cts;
    private static readonly ConcurrentQueue<LogEntry> _recentLogs = new ConcurrentQueue<LogEntry>();
    private const int MaxLogHistory = 50;

    [Serializable]
    public struct LogEntry
    {
        public string type;
        public string message;
        public string stackTrace;
        public string timestamp;
    }

    static AntigravityBridge()
    {
        Application.logMessageReceived += OnLogReceived;
        AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        EditorApplication.quitting += StopServer;

        EditorApplication.delayCall += StartServer;
    }

    private static void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
        {
            _recentLogs.Enqueue(new LogEntry
            {
                type = type.ToString(),
                message = condition,
                stackTrace = stackTrace,
                timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
            });

            while (_recentLogs.Count > MaxLogHistory)
            {
                _recentLogs.TryDequeue(out _);
            }
        }
    }

    private static void StartServer()
    {
        StopServer();

        try
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            Task.Run(() => ListenLoop(_cts.Token));
            Debug.Log($"<color=#4CAF50>[AntigravityBridge]</color> Active at http://localhost:{Port}/");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AntigravityBridge] Could not start HTTP listener on port {Port}: {ex.Message}");
        }
    }

    private static void StopServer()
    {
        try
        {
            _cts?.Cancel();
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
        catch { }
        finally
        {
            _listener = null;
            _cts = null;
        }
    }

    private static async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntigravityBridge] Listener error: {ex.Message}");
            }
        }
    }

    private static void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var req = context.Request;
            var path = req.Url.AbsolutePath.ToLowerInvariant();
            string responseJson = "{\"status\":\"ok\"}";
            int statusCode = 200;

            switch (path)
            {
                case "/refresh":
                    EditorApplication.delayCall += () =>
                    {
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    };
                    responseJson = "{\"status\":\"refreshing\",\"message\":\"AssetDatabase.Refresh triggered\"}";
                    break;

                case "/status":
                    responseJson = string.Format(
                        "{{\"status\":\"ok\",\"isCompiling\":{0},\"isPlaying\":{1},\"isPaused\":{2},\"unityVersion\":\"{3}\"}}",
                        EditorApplication.isCompiling.ToString().ToLowerInvariant(),
                        EditorApplication.isPlaying.ToString().ToLowerInvariant(),
                        EditorApplication.isPaused.ToString().ToLowerInvariant(),
                        Application.unityVersion
                    );
                    break;

                case "/console":
                    var logs = _recentLogs.ToArray();
                    var sb = new StringBuilder();
                    sb.Append("{\"logs\":[");
                    for (int i = 0; i < logs.Length; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append(JsonUtility.ToJson(logs[i]));
                    }
                    sb.Append("]}");
                    responseJson = sb.ToString();
                    break;

                case "/clear-console":
                    while (_recentLogs.TryDequeue(out _)) { }
                    responseJson = "{\"status\":\"cleared\"}";
                    break;

                default:
                    statusCode = 404;
                    responseJson = "{\"error\":\"Endpoint not found. Available: /refresh, /status, /console, /clear-console\"}";
                    break;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Flush();
            context.Response.Close();
        }
        catch { }
    }
}
#endif
