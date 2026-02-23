using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace shakeyourbunny.CredentialManager;

/// <summary>
/// Safe handle wrapper for unmanaged credential memory allocated by the Windows Credential API.
/// Ensures native credential buffers (including sensitive credential blobs) are securely zeroed
/// before being freed.
/// </summary>
sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
{
    private readonly uint _enumerationCount;

    /// <summary>
    /// Creates a handle for a single credential (from CredRead).
    /// </summary>
    internal CriticalCredentialHandle(IntPtr preexistingHandle)
    {
        SetHandle(preexistingHandle);
        _enumerationCount = 0;
    }

    /// <summary>
    /// Creates a handle for an enumeration result (from CredEnumerate).
    /// </summary>
    /// <param name="preexistingHandle">Pointer to the credential array.</param>
    /// <param name="enumerationCount">Number of credentials in the array.</param>
    internal CriticalCredentialHandle(IntPtr preexistingHandle, uint enumerationCount)
    {
        SetHandle(preexistingHandle);
        _enumerationCount = enumerationCount;
    }

    /// <summary>
    /// Reads a single credential from the native handle.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
    internal Credential GetCredential()
    {
        if (!IsInvalid)
        {
            var ncred = (NativeCode.NativeCredential)Marshal.PtrToStructure(handle,
                  typeof(NativeCode.NativeCredential))!;

            return new Credential(ncred);
        }
        else
        {
            throw new InvalidOperationException(SR.InvalidCriticalHandle);
        }
    }

    /// <summary>
    /// Reads multiple credentials from the native handle (enumeration result).
    /// </summary>
    /// <param name="size">Number of credentials to read.</param>
    /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
    internal Credential[] EnumerateCredentials(uint size)
    {
        if (!IsInvalid)
        {
            var credentialArray = new Credential[size];

            for (int i = 0; i < size; i++)
            {
                IntPtr ptrPlc = Marshal.ReadIntPtr(handle, i * IntPtr.Size);

                var nc = (NativeCode.NativeCredential)Marshal.PtrToStructure(ptrPlc,
                    typeof(NativeCode.NativeCredential))!;

                credentialArray[i] = new Credential(nc);
            }

            return credentialArray;
        }
        else
        {
            throw new InvalidOperationException(SR.InvalidCriticalHandle);
        }
    }

    /// <summary>
    /// Securely zeros credential blobs in native memory, then frees the handle via CredFree.
    /// Uses RtlZeroMemory via P/Invoke to prevent JIT dead store elimination.
    /// </summary>
    override protected bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            // Zero credential blobs before freeing — prevents sensitive data from
            // lingering in freed memory pages.
            ZeroCredentialBlobs();

            NativeCode.CredFree(handle);
            SetHandleAsInvalid();
            return true;
        }
        return false;
    }

    private void ZeroCredentialBlobs()
    {
        try
        {
            if (_enumerationCount == 0)
            {
                // Single credential (from CredRead)
                ZeroSingleCredentialBlob(handle);
            }
            else
            {
                // Enumeration (from CredEnumerate) — handle is array of pointers
                for (uint i = 0; i < _enumerationCount; i++)
                {
                    IntPtr ptr = Marshal.ReadIntPtr(handle, (int)(i * (uint)IntPtr.Size));
                    ZeroSingleCredentialBlob(ptr);
                }
            }
        }
        catch
        {
            // Best-effort zeroing — don't prevent CredFree on failure
        }
    }

    private static void ZeroSingleCredentialBlob(IntPtr credPtr)
    {
        var ncred = (NativeCode.NativeCredential)Marshal.PtrToStructure(credPtr,
            typeof(NativeCode.NativeCredential))!;

        if (ncred.CredentialBlob != IntPtr.Zero && ncred.CredentialBlobSize > 0)
        {
            NativeCode.SecureZeroMemory(ncred.CredentialBlob, new UIntPtr(ncred.CredentialBlobSize));
        }
    }
}
