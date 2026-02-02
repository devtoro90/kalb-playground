using System.Collections.Generic;
using UnityEngine;

public class KalbInputBuffer : MonoBehaviour
{
    [System.Serializable]
    public class BufferedInput
    {
        public string inputType;
        public float timestamp;
        public float duration;
        public bool consumed;
        
        public bool IsValid => !consumed && (Time.time - timestamp) <= duration;
    }
    
    [Header("Buffer Settings")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float dashBufferTime = 0.1f;
    [SerializeField] private float attackBufferTime = 0.1f;
    
    private List<BufferedInput> bufferedInputs = new List<BufferedInput>();
    
    // Public methods
    public void BufferJump()
    {
        AddOrUpdateBuffer("Jump", jumpBufferTime);
    }
    
    public void BufferDash()
    {
        AddOrUpdateBuffer("Dash", dashBufferTime);
    }
    
    public void BufferAttack()
    {
        AddOrUpdateBuffer("Attack", attackBufferTime);
    }
    
    public bool ConsumeBufferedInput(string inputType)
    {
        for (int i = bufferedInputs.Count - 1; i >= 0; i--)
        {
            if (bufferedInputs[i].inputType == inputType && bufferedInputs[i].IsValid)
            {
                bufferedInputs[i].consumed = true;
                CleanupBuffers();
                return true;
            }
        }
        return false;
    }
    
    public bool HasBufferedInput(string inputType)
    {
        foreach (var buffer in bufferedInputs)
        {
            if (buffer.inputType == inputType && buffer.IsValid)
            {
                return true;
            }
        }
        return false;
    }
    
    public void ClearBufferedInput(string inputType = null)
    {
        if (inputType == null)
        {
            // Clear all
            bufferedInputs.Clear();
        }
        else
        {
            // Clear specific type
            for (int i = bufferedInputs.Count - 1; i >= 0; i--)
            {
                if (bufferedInputs[i].inputType == inputType)
                {
                    bufferedInputs.RemoveAt(i);
                }
            }
        }
    }
    
    public void ClearAllBuffersOnStateChange()
    {
        ClearBufferedInput();
    }
    
    private void AddOrUpdateBuffer(string inputType, float duration)
    {
        // Check for existing buffer of same type
        for (int i = 0; i < bufferedInputs.Count; i++)
        {
            if (bufferedInputs[i].inputType == inputType)
            {
                bufferedInputs[i].timestamp = Time.time;
                bufferedInputs[i].duration = duration;
                bufferedInputs[i].consumed = false;
                return;
            }
        }
        
        // Add new buffer
        bufferedInputs.Add(new BufferedInput
        {
            inputType = inputType,
            timestamp = Time.time,
            duration = duration,
            consumed = false
        });
    }
    
    private void CleanupBuffers()
    {
        // Remove expired or consumed buffers
        for (int i = bufferedInputs.Count - 1; i >= 0; i--)
        {
            if (!bufferedInputs[i].IsValid)
            {
                bufferedInputs.RemoveAt(i);
            }
        }
    }
    
    private void Update()
    {
        CleanupBuffers();
    }
    
    // Debug visualization
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 100, 200, 200));
        GUILayout.Label("Input Buffer:");
        
        foreach (var buffer in bufferedInputs)
        {
            string status = buffer.consumed ? "CONSUMED" : "ACTIVE";
            float remaining = Mathf.Max(0, buffer.duration - (Time.time - buffer.timestamp));
            GUILayout.Label($"{buffer.inputType}: {status} ({remaining:F2}s)");
        }
        
        GUILayout.EndArea();
    }
}