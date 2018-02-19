/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Alm.Authentication
{
    internal static class NativeMethods
    {
        private const string Advapi32 = "advapi32.dll";
        private const string Kernel32 = "kernel32.dll";

        /// <summary>
        /// <para>
        /// The CredWrite function creates a new credential or modifies an existing credential in the
        /// user's credential set.
        /// </para>
        /// <para>The new credential is associated with the logon session of the current token.</para>
        /// <para>The token must not have the user's security identifier (SID) disabled.</para>
        /// </summary>
        /// <param name="credential">
        /// A pointer to the `<see cref="Authentication.Credential"/>` structure to be written.
        /// </param>
        /// <param name="flags">Flags that control the function's operation. Must be set to 0.</param>
        /// <returns>True if success; false otherwise.</returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredWriteW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredWrite(
            [In] ref Credential credential,
            [In] uint flags);

        /// <summary>
        /// <para>Reads a credential from the user's credential set.</para>
        /// <para>
        /// The credential set used is the one associated with the logon session of the current token.
        /// </para>
        /// <para>The token must not have the user's SID disabled.</para>
        /// </summary>
        /// <param name="targetName">
        /// Pointer to a null-terminated string that contains the name of the credential to read.
        /// </param>
        /// <param name="type">
        /// Type of the credential to read. Type must be one of the ` <see cref="CredentialType"/>`
        /// defined types.
        /// </param>
        /// <param name="flags">Currently reserved and must be zero.</param>
        /// <param name="credential">
        /// <para>Pointer to a single allocated block buffer to return the credential.</para>
        /// <para>
        /// Any pointers contained within the buffer are pointers to locations within this single
        /// allocated block.
        /// </para>
        /// <para>The single returned buffer must be freed by calling ` <see cref="CredFree(IntPtr)"/>`.</para>
        /// </param>
        /// <returns>True if success; false otherwise.</returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredReadW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredRead(
            [In][MarshalAs(UnmanagedType.LPWStr)] string targetName,
            [In][MarshalAs(UnmanagedType.U4)] CredentialType type,
            [In] uint flags,
            [Out] out IntPtr credential);

        /// <summary>
        /// <para>The CredDelete function deletes a credential from the user's credential set.</para>
        /// <para>
        /// The credential set used is the one associated with the logon session of the current token.
        /// </para>
        /// <para>The token must not have the user's SID disabled.</para>
        /// </summary>
        /// <param name="targetName">
        /// Pointer to a null-terminated string that contains the name of the credential to delete.
        /// </param>
        /// <param name="type">
        /// <para>
        /// Type of the credential to delete. Must be one of the ` <see cref="CredentialType"/>`
        /// defined types. For a list of the defined types, see the Type member of the `
        /// <see cref="Credential"/>` structure.
        /// </para>
        /// <para>
        /// If the value of this parameter is ` <see cref="CredentialType.DomainExtended"/>`, this
        /// function can delete a credential that specifies a user name when there are multiple
        /// credentials for the same target, and the value of the `
        /// <see cref="Credential.TargetName"/>` parameter must specify the user name as Target|UserName.
        /// </para>
        /// </param>
        /// <param name="flags">Reserved and must be zero.</param>
        /// <returns>True if success; false otherwise.</returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredDeleteW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredDelete(
            [In][MarshalAs(UnmanagedType.LPWStr)] string targetName,
            [In][MarshalAs(UnmanagedType.U4)] CredentialType type,
            [In] uint flags);

        /// <summary>
        /// The CredFree function frees a buffer returned by any of the credentials
        /// management functions.
        /// </summary>
        /// <param name="credential">Pointer to the buffer to be freed.</param>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredFree", SetLastError = true)]
        internal static extern void CredFree(
            [In] IntPtr credential);

        /// <summary>
        /// Enumerates the credentials from the user's credential set. The credential set used is the
        /// one associated with the logon session of the current token. The token must not have the
        /// user's SID disabled.
        /// </summary>
        /// <param name="targetNameFilter">
        /// <para>
        /// Pointer to a null-terminated string that contains the filter for the returned
        /// credentials. Only credentials with a TargetName matching the filter will be returned. The
        /// filter specifies a name prefix followed by an asterisk. For instance, the filter "FRED*"
        /// will return all credentials with a TargetName beginning with the string "FRED".
        /// </para>
        /// <para>If <see langword="null"/> is specified, all credentials will be returned.</para>
        /// </param>
        /// <param name="flags">
        /// The value of this parameter can be zero or more values combined with a bitwise-OR operation.
        /// </param>
        /// <param name="count">Count of the credentials returned in the <paramref name="credenitalsArrayPtr"/>.</param>
        /// <param name="credenitalsArrayPtr">
        /// <para>
        /// Pointer to an array of pointers to credentials. The returned credential is a single
        /// allocated block. Any pointers contained within the buffer are pointers to locations
        /// within this single allocated block.
        /// </para>
        /// <para>The single returned buffer must be freed by calling <see cref="CredFree"/>.</para>
        /// </param>
        /// <returns></returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredEnumerateW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredEnumerate(
            [In][MarshalAs(UnmanagedType.LPWStr)] string targetNameFilter,
            [In][MarshalAs(UnmanagedType.U4)] CredentialEnumerateFlags flags,
            [Out] out int count,
            [Out] out IntPtr credenitalsArrayPtr);

        [Flags]
        internal enum CredentialEnumerateFlags : uint
        {
            None = 0,
            AllCredentials = 1 << 0,
        }

        [Flags]
        internal enum CredentialFlags : uint
        {
            /// <summary>
            /// Bit set if the ` <see cref="Credential"/>` does not persist the `<see cref="Credential.CredentialBlob"/>` and the credential has not been written
            /// during this logon session. This bit is ignored on input and is set automatically when queried.
            /// <para />
            /// If `<see cref="Credential.Type"/>` is `<see cref="CredentialType.DomainCertificate"/>`, the `<see cref="Credential.CredentialBlob"/>` is not persisted across logon sessions
            /// because the PIN of a certificate is very sensitive information.
            /// <para />
            /// Indeed, when the credential is written to credential manager, the PIN is passed to
            /// the CSP associated with the certificate. The CSP will enforce a PIN retention policy
            /// appropriate to the certificate.
            /// <para />
            /// If Type is `<see cref="CredentialType.DomainPassword"/>` or `<see cref="CredentialType.DomainCertificate"/>`, an authentication package always
            /// fails an authentication attempt when using credentials marked as `<see cref="CredentialFlags.PromptNow"/>`.
            /// <para />
            /// The application (typically through the key ring UI) prompts the user for the
            /// password. The application saves the credential and retries the authentication.
            /// <para />
            /// Because the credential has been recently written, the authentication package now gets
            /// a credential that is not marked as `<see cref="CredentialFlags.PromptNow"/>`.
            /// </summary>
            PromptNow = 0x02,

            /// <summary>
            /// Bit is set if this `<see cref="Credential"/>` has a `<see cref="Credential.TargetName"/>` member set to the same value as the `<see cref="Credential.UserName"/>` member.
            /// <para />
            /// Such a credential is one designed to store the `<see cref="Credential.CredentialBlob"/>` for a specific user.
            /// <para />
            /// This bit can only be specified if ` <see cref="Credential.Type"/>` is `<see cref="CredentialType.DomainPassword"/>` or `<see cref="CredentialType.DomainCertificate"/>`.
            /// </summary>
            UsernameTarget = 0x04,
        }

        internal enum CredentialPersist : uint
        {
            /// <summary>
            /// <para>The `<see cref="Credential"/>` persists for the life of the logon session.</para>
            /// <para>It will not be visible to other logon sessions of this same user.</para>
            /// <para>It will not exist after this user logs off and back on.</para>
            /// </summary>
            Session = 0x01,

            /// <summary>
            /// <para>
            /// The `<see cref="Credential"/>` persists for all subsequent logon sessions on this
            /// same computer.
            /// </para>
            /// <para>
            /// It is visible to other logon sessions of this same user on this same computer and not
            /// visible to logon sessions for this user on other computers.
            /// </para>
            /// </summary>
            /// <remarks>
            /// Windows Vista Home Basic, Windows Vista Home Premium, Windows Vista Starter, and
            /// Windows XP Home Edition: This value is not supported.
            /// </remarks>
            LocalMachine = 0x02,

            /// <summary>
            /// <para>
            /// The `<see cref="Credential"/>` persists for all subsequent logon sessions on this
            /// same computer.
            /// </para>
            /// <para>
            /// It is visible to other logon sessions of this same user on this same computer and to
            /// logon sessions for this user on other computers.
            /// </para>
            /// </summary>
            /// <remarks>
            /// Windows Vista Home Basic, Windows Vista Home Premium, Windows Vista Starter, and
            /// Windows XP Home Edition: This value is not supported.
            /// </remarks>
            Enterprise = 0x03
        }

        internal enum CredentialType : uint
        {
            /// <summary> 
            /// The `<see cref="Credential"/>` is a generic credential. The
            /// credential will not be used by any particular authentication package.
            /// <para />
            /// The credential will be stored securely but has no other significant
            /// characteristics.
            /// </summary>
            Generic = 0x01,

            /// <summary>
            /// The `<see cref="Credential"/>` is a password credential and is specific to
            /// Microsoft's authentication packages.
            /// <para />
            /// The NTLM, Kerberos, and Negotiate authentication packages will automatically use this
            /// credential when connecting to the named target.
            /// </summary>
            DomainPassword = 0x02,

            /// <summary>
            /// The `<see cref="Credential"/>` is a certificate credential and is specific to
            /// Microsoft's authentication packages.
            /// <para />
            /// The Kerberos, Negotiate, and SChannel authentication packages automatically use this
            /// credential when connecting to the named target.
            /// </summary>
            DomainCertificate = 0x03,

            /// <summary>
            /// The `<see cref="Credential"/>` is a password credential and is specific to
            /// authentication packages from Microsoft.
            /// <para />
            /// The Passport authentication package will automatically use this credential when
            /// connecting to the named target.
            /// </summary>
            [Obsolete("This value is no longer supported", true)]
            DomainVisiblePassword = 0x04,

            /// <summary>
            /// The `<see cref="Credential"/>` is a certificate credential that is a generic
            /// authentication package.
            /// </summary>
            GenericCertificate = 0x05,

            /// <summary>
            /// The `<see cref="Credential"/>` is supported by extended Negotiate packages.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This value
            /// is not supported.
            /// </remarks>
            DomainExtended = 0x06,

            /// <summary>
            /// The maximum number of supported credential types.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:
            /// This value is not supported.
            /// </remarks>
            Maximum = 0x07,

            /// <summary>
            /// The extended maximum number of supported credential types that now allow new
            /// applications to run on older operating systems.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This value
            /// is not supported.
            /// </remarks>
            MaximumEx = Maximum + 1000
        }

        [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct Credential
        {
            internal const int AttributeMaxLengh = 63;
            internal const int PasswordMaxLength = 2047;
            internal const int StringMaxLength = 255;
            internal const int UsernameMaxLength = 511;

            /// <summary>
            /// A bit member that identifies characteristics of the credential.
            /// <para />
            /// Undefined bits should be initialized as zero and not otherwise altered to permit
            /// future enhancement.
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public CredentialFlags Flags;

            /// <summary>
            /// The type of the credential. This member cannot be changed after the credential is created.
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public CredentialType Type;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;

            /// <summary>
            /// A string comment from the user that describes this credential.
            /// <para />
            /// This member cannot be longer than `<see cref="StringMaxLength"/>` characters.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;

            /// <summary>
            /// The time, in Coordinated Universal Time (Greenwich Mean Time), of the last
            /// modification of the credential.
            /// <para />
            /// For write operations, the value of this member is ignored.
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;

            /// <summary>
            /// The size, in bytes, of the `<see cref="CredentialBlob"/>` member.
            /// <para />
            /// This member cannot be larger than `<see cref="PasswordMaxLength"/>` bytes.
            /// </summary>
            public uint CredentialBlobSize;

            /// <summary>
            /// Secret data for the credential. The CredentialBlob member can be both read and written.
            /// <para />
            /// If the `<see cref="Type"/>` member is `<see cref="CredentialType.DomainPassword"/>`, this member contains the plain-text
            /// Unicode password for `<see cref="UserName"/>`.
            /// <para />
            /// The ` <see cref="CredentialBlob"/>` and ` <see cref="CredentialBlobSize"/>` members
            /// do not include a trailing zero character.
            /// <para />
            /// Also, for ` <see cref="CredentialType.DomainPassword"/>`, this member can only be
            /// read by the authentication packages.
            /// <para />
            /// If the Type member is ` <see cref="CredentialType.DomainCertificate"/>`, this member
            /// contains the clear test Unicode PIN for ` <see cref="UserName"/>`.
            /// <para />
            /// The ` <see cref="CredentialBlob"/>` and ` <see cref="CredentialBlobSize"/>` members
            /// do not include a trailing zero character.
            /// <para />
            /// Also, this member can only be read by the authentication packages.
            /// <para />
            /// If the ` <see cref="Type"/>` member is `<see cref="CredentialType.Generic"/>`, this
            /// member is defined by the application.
            /// <para />
            /// Credentials are expected to be portable. Applications should ensure that the data in
            /// ` <see cref="CredentialBlob"/>` is portable.
            /// </summary>
            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            public IntPtr CredentialBlob;

            /// <summary>
            /// Defines the persistence of this credential. This member can be read and written.
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public CredentialPersist Persist;

            /// <summary>
            /// The number of application-defined attributes to be associated with the credential.
            /// <para />
            /// This member can be read and written. Its value cannot be greater than ` <see cref="AttributeMaxLengh"/>`.
            /// </summary>
            public uint AttributeCount;

            /// <summary>
            /// Application-defined attributes that are associated with the credential. This member
            /// can be read and written.
            /// </summary>
            public IntPtr Attributes;

            /// <summary>
            /// Alias for the TargetName member. This member can be read and written. It cannot be
            /// longer than `<see cref="StringMaxLength"/>` characters.
            /// <para />
            /// If the credential Type is `<see cref="CredentialType.Generic"/>`, this member can be
            /// non-NULL, but the credential manager ignores the member.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetAlias;

            /// <summary>
            /// The user name of the account used to connect to TargetName.
            /// <para />
            /// If the credential Type is `<see cref="CredentialType.DomainPassword"/>`, this member
            /// can be either a DomainName\UserName or a UPN.
            /// <para />
            /// If the credential Type is `<see cref="CredentialType.DomainCertificate"/>`, this
            /// member must be a marshaled certificate reference created by calling
            /// `CredMarshalCredential` with a `CertCredential`.
            /// <para />
            /// If the credential Type is `<see cref="CredentialType.Generic"/>`, this member can be
            /// non-NULL, but the credential manager ignores the member.
            /// <para />
            /// This member cannot be longer than CRED_MAX_USERNAME_LENGTH(513) characters.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserName;
        }

        /// <summary>
        /// Closes an open object handle.
        /// <para />
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CloseHandle", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(
            [In] IntPtr handle);

        /// <summary>
        /// Closes an open object handle.
        /// <para />
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CloseHandle", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(
            [In] SafeHandle handle);

        /// <summary>
        /// The System Error Codes are very broad. Each one can occur in one of many hundreds of
        /// locations in the system. Consequently the descriptions of these codes cannot be very
        /// specific. Use of these codes requires some amount of investigation and analysis. You need
        /// to note both the programmatic and the run-time context in which these errors occur.
        /// Because these codes are defined in WinError.h for anyone to use, sometimes the codes are
        /// returned by non-system software. Sometimes the code is returned by a function deep in the
        /// stack and far removed from your code that is handling the error.
        /// </summary>
        internal static class Win32Error
        {
            /// <summary>
            /// The operation completed successfully.
            /// </summary>
            public const int Success = 0;

            /// <summary>
            /// The system cannot find the file specified.
            /// </summary>
            public const int FileNotFound = 2;

            /// <summary>
            /// The handle is invalid.
            /// </summary>
            public const int InvalidHandle = 6;

            /// <summary>
            /// Not enough storage is available to process this command.
            /// </summary>
            public const int NotEnoughMemory = 8;

            /// <summary>
            /// The process cannot access the file because it is being used by another process.
            /// </summary>
            public const int SharingViloation = 32;

            /// <summary>
            /// The file exists.
            /// </summary>
            public const int FileExists = 80;

            /// <summary>
            /// Cannot create a file when that file already exists.
            /// </summary>
            public const int AlreadExists = 183;

            /// <summary>
            /// Element not found.
            /// </summary>
            public const int NotFound = 1168;

            /// <summary>
            /// A specified logon session does not exist. It may already have been terminated.
            /// </summary>
            public const int NoSuchLogonSession = 1312;
        }
    }
}
