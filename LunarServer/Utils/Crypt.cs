using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace LunarLabs.WebServer.Utils
{
    public class Crypt
    {
            public static string crypt(string key, string salt)
            {
                ArrayPointer<byte> key_ptr = new ArrayPointer<byte>(
                    Encoding.UTF8.GetBytes(key + "\0"));

                ArrayPointer<byte> salt_ptr = new ArrayPointer<byte>(
                    Encoding.UTF8.GetBytes(salt + "\0"));

                /* Try to find out whether we have to use MD5 encryption replacement.  */
                if (strncmp(md5_salt_prefix, salt_ptr, strlen(md5_salt_prefix)) == 0)
                    return ExtractString(__md5_crypt(key_ptr, salt_ptr));

                /* Try to find out whether we have to use SHA256 encryption replacement.  */
                if (strncmp(sha256_salt_prefix, salt_ptr, strlen(sha256_salt_prefix)) == 0)
                    return ExtractString(__sha256_crypt(key_ptr, salt_ptr));

                /* Try to find out whether we have to use SHA512 encryption replacement.  */
                //if (strncmp (sha512_salt_prefix, salt_ptr, strlen(sha512_salt_prefix)) == 0)
                //    return __sha512_crypt (key_ptr, salt_ptr);

                //return __crypt_r (key_ptr, salt_ptr, &_ufc_foobar);

                return string.Empty;
            }

            /* Table with characters for base64 transformation.  */
            static char[] b64t =
                "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

            /* Define our magic string to mark salt for MD5 "encryption"
               replacement.  This is meant to be the same as for other MD5 based
               encryption implementations.  */
            static ArrayPointer<byte> md5_salt_prefix = new ArrayPointer<byte>(new byte[] { (byte)'$', (byte)'1', (byte)'$', 0 });
            static ArrayPointer<byte> dollar_sign = new ArrayPointer<byte>(new byte[] { (byte)'$', 0 });

            /* Define our magic string to mark salt for SHA256 "encryption"
               replacement.  */
            static ArrayPointer<byte> sha256_salt_prefix = new ArrayPointer<byte>(new byte[] { (byte)'$', (byte)'5', (byte)'$', 0 });

            /* Prefix for optional rounds specification.  */
            static ArrayPointer<byte> sha256_rounds_prefix = new ArrayPointer<byte>(new byte[] { (byte)'r', (byte)'o', (byte)'u', (byte)'n', (byte)'d', (byte)'s', (byte)'=', 0 });

            /* Maximum salt string length.  */
            const int SALT_LEN_MAX = 16;

            /* Default number of rounds if not explicitly specified.  */
            const int ROUNDS_DEFAULT = 5000;

            /* Minimum number of rounds.  */
            const int ROUNDS_MIN = 1000;

            /* Maximum number of rounds.  */
            const int ROUNDS_MAX = 999999999;

            private static ArrayPointer<byte> __md5_crypt(ArrayPointer<byte> key, ArrayPointer<byte> salt)
            {
                /* We don't want to have an arbitrary limit in the size of the
                   password.  We can compute the size of the result in advance and
                   so we can prepare the buffer we pass to `md5_crypt_r'.  */
                ArrayPointer<byte> buffer;
                int buflen = 0;
                int needed = 3 + strlen(salt) + 1 + 26 + 1;

                ArrayPointer<byte> new_buffer = new ArrayPointer<byte>(new byte[needed]);

                buffer = new_buffer;
                buflen = needed;

                return __md5_crypt_r(key, salt, buffer, buflen);
            }

            private static ArrayPointer<byte> __sha256_crypt(ArrayPointer<byte> key, ArrayPointer<byte> salt)
            {
                /* We don't want to have an arbitrary limit in the size of the
                   password.  We can compute an upper bound for the size of the
                   result in advance and so we can prepare the buffer we pass to
                   `sha256_crypt_r'.  */
                ArrayPointer<byte> buffer;
                int buflen = 0;
                int needed = (strlen(sha256_salt_prefix)
                      + strlen(sha256_rounds_prefix) + 1 + 9 + 1
                      + strlen(salt) + 1 + 43 + 1);

                ArrayPointer<byte> new_buffer = new ArrayPointer<byte>(new byte[needed]);

                buffer = new_buffer;
                buflen = needed;

                return __sha256_crypt_r(key, salt, buffer, buflen);
            }

            public static int strlen(ArrayPointer<byte> str)
            {
                if (str == null)
                {
                    return 0;
                }

                for (int i = 0; ; i++, str++)
                {
                    if (str.Value == 0)
                        return i;
                }
            }

            public static int strncmp(ArrayPointer<byte> str1, ArrayPointer<byte> str2, int length)
            {
                for (int i = 0; i < length; i++, str1++, str2++)
                {
                    if (str1.Value > str2.Value)
                    {
                        return 1;
                    }
                    else if (str2.Value > str1.Value)
                    {
                        return -1;
                    }
                }

                return 0;
            }

            private static int strcspn(ArrayPointer<byte> str1, ArrayPointer<byte> str2)
            {
                int location = 0;
                ArrayPointer<byte> i;
                ArrayPointer<byte> j;
                int str1_len = strlen(str1);
                int str2_len = strlen(str2);

                for (i = str1; i.Value != 0; i++, location++)
                {
                    for (j = str2; j.Value != 0; j++)
                    {
                        if (i.Value == j.Value)
                            return location;
                    }
                }

                return str1_len;
            }

            private class md5_ctx
            {
                public MemoryStream stream;
            }

            private class sha256_ctx
            {
                public MemoryStream stream;
            }

            private static ArrayPointer<byte> __md5_crypt_r(ArrayPointer<byte> key, ArrayPointer<byte> salt, ArrayPointer<byte> buffer, int buflen)
            {
                ArrayPointer<byte> alt_result = new ArrayPointer<byte>(new byte[16]);
                md5_ctx ctx = new md5_ctx();
                md5_ctx alt_ctx = new md5_ctx();
                int salt_len;
                int key_len;
                int cnt;
                ArrayPointer<byte> cp;
                ArrayPointer<byte> copied_key;
                ArrayPointer<byte> copied_salt;

                /* Find beginning of salt string.  The prefix should normally always
                be present.  Just in case it is not.  */
                if (strncmp(md5_salt_prefix, salt, strlen(md5_salt_prefix)) == 0)
                {
                    salt += strlen(md5_salt_prefix);
                }

                salt_len = Math.Min(strcspn(salt, dollar_sign), 8);
                key_len = strlen(key);

                byte[] temp = new byte[key.SourceArray.Length];
                key.SourceArray.CopyTo(temp, 0);
                copied_key = new ArrayPointer<byte>(temp);
                copied_key.Address = key.Address;
                key = copied_key;

                temp = new byte[salt.SourceArray.Length];
                salt.SourceArray.CopyTo(temp, 0);
                copied_salt = new ArrayPointer<byte>(temp);
                copied_salt.Address = salt.Address;
                salt = copied_salt;

                /* Prepare for the real work.  */
                __md5_init_ctx(ctx);

                /* Add the key string.  */
                __md5_process_bytes(key, key_len, ctx);

                /* Because the SALT argument need not always have the salt prefix we
                   add it separately.  */
                __md5_process_bytes(md5_salt_prefix, strlen(md5_salt_prefix), ctx);

                /* The last part is the salt string.  This must be at most 8
                   characters and it ends at the first `$' character (for
                   compatibility with existing implementations).  */
                __md5_process_bytes(salt, salt_len, ctx);


                /* Compute alternate MD5 sum with input KEY, SALT, and KEY.  The
                   final result will be added to the first context.  */
                __md5_init_ctx(alt_ctx);

                /* Add key.  */
                __md5_process_bytes(key, key_len, alt_ctx);

                /* Add salt.  */
                __md5_process_bytes(salt, salt_len, alt_ctx);

                /* Add key again.  */
                __md5_process_bytes(key, key_len, alt_ctx);

                /* Now get result of this (16 bytes) and add it to the other
                context.  */
                __md5_finish_ctx(alt_ctx, alt_result);

                /* Add for any character in the key one byte of the alternate sum.  */
                for (cnt = key_len; cnt > 16; cnt -= 16)
                    __md5_process_bytes(alt_result, 16, ctx);
                __md5_process_bytes(alt_result, cnt, ctx);

                /* For the following code we need a NUL byte.  */
                alt_result.Value = 0;

                /* The original implementation now does something weird: for every 1
                   bit in the key the first 0 is added to the buffer, for every 0
                   bit the first character of the key.  This does not seem to be
                   what was intended but we have to follow this to be compatible.  */
                for (cnt = key_len; cnt > 0; cnt >>= 1)
                    __md5_process_bytes((cnt & 1) != 0 ? alt_result : key, 1, ctx);

                /* Create intermediate result.  */
                __md5_finish_ctx(ctx, alt_result);

                /* Now comes another weirdness.  In fear of password crackers here
                   comes a quite long loop which just processes the output of the
                   previous round again.  We cannot ignore this here.  */
                for (cnt = 0; cnt < 1000; ++cnt)
                {
                    /* New context.  */
                    __md5_init_ctx(ctx);

                    /* Add key or last result.  */
                    if ((cnt & 1) != 0)
                        __md5_process_bytes(key, key_len, ctx);
                    else
                        __md5_process_bytes(alt_result, 16, ctx);

                    /* Add salt for numbers not divisible by 3.  */
                    if (cnt % 3 != 0)
                        __md5_process_bytes(salt, salt_len, ctx);

                    /* Add key for numbers not divisible by 7.  */
                    if (cnt % 7 != 0)
                        __md5_process_bytes(key, key_len, ctx);

                    /* Add key or last result.  */
                    if ((cnt & 1) != 0)
                        __md5_process_bytes(alt_result, 16, ctx);
                    else
                        __md5_process_bytes(key, key_len, ctx);

                    /* Create intermediate result.  */
                    __md5_finish_ctx(ctx, alt_result);
                }

                /* Now we can construct the result string.  It consists of three
                   parts.  */
                cp = __stpncpy(buffer, md5_salt_prefix, Math.Max(0, buflen));
                buflen -= strlen(md5_salt_prefix);

                cp = __stpncpy(cp, salt, Math.Min(Math.Max(0, buflen), salt_len));
                buflen -= Math.Min(Math.Max(0, buflen), salt_len);

                if (buflen > 0)
                {
                    cp.Value = (byte)'$';
                    cp++;
                    buflen--;
                }

                cp = b64_from_24bit(alt_result[0], alt_result[6], alt_result[12], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[1], alt_result[7], alt_result[13], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[2], alt_result[8], alt_result[14], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[3], alt_result[9], alt_result[15], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[4], alt_result[10], alt_result[5], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(0, 0, alt_result[11], 2, cp, buflen, out buflen);

                if (buflen <= 0)
                {
                    throw new IndexOutOfRangeException();
                }
                else
                    cp.Value = 0;       /* Terminate the string.  */

                /* Clear the buffer for the intermediate result so that people
                attaching to processes or reading core dumps cannot get any
                information.  We do it in this way to clear correct_words[]
                inside the MD5 implementation as well.  */
                __md5_init_ctx(ctx);
                __md5_finish_ctx(ctx, alt_result);

                return buffer;
            }

            private static ArrayPointer<byte> __sha256_crypt_r(ArrayPointer<byte> key, ArrayPointer<byte> salt, ArrayPointer<byte> buffer, int buflen)
            {
                ArrayPointer<byte> alt_result = new ArrayPointer<byte>(new byte[32]);
                ArrayPointer<byte> temp_result = new ArrayPointer<byte>(new byte[32]);
                sha256_ctx ctx = new sha256_ctx();
                sha256_ctx alt_ctx = new sha256_ctx();
                int salt_len;
                int key_len;
                int cnt;
                ArrayPointer<byte> cp;
                ArrayPointer<byte> copied_key;
                ArrayPointer<byte> copied_salt;
                ArrayPointer<byte> p_bytes;
                ArrayPointer<byte> s_bytes;
                /* Default number of rounds.  */
                int rounds = ROUNDS_DEFAULT;
                bool rounds_custom = false;

                /* Find beginning of salt string.  The prefix should normally always
                   be present.  Just in case it is not.  */
                if (strncmp(sha256_salt_prefix, salt, strlen(sha256_salt_prefix)) == 0)
                    /* Skip salt prefix.  */
                    salt += strlen(sha256_salt_prefix);

                if (strncmp(salt, sha256_rounds_prefix, strlen(sha256_rounds_prefix)) == 0)
                {
                    ArrayPointer<byte> num = salt + strlen(sha256_rounds_prefix);
                    ArrayPointer<byte> endp;
                    ulong srounds = strtoul(num, out endp, 10);
                    if (endp.Value == (byte)'$')
                    {
                        salt = endp + 1;
                        rounds = (int)Math.Max(ROUNDS_MIN, Math.Min(srounds, ROUNDS_MAX));
                        rounds_custom = true;
                    }
                }

                salt_len = Math.Min(strcspn(salt, dollar_sign), SALT_LEN_MAX);
                key_len = strlen(key);

                byte[] temp = new byte[key.SourceArray.Length];
                key.SourceArray.CopyTo(temp, 0);
                copied_key = new ArrayPointer<byte>(temp);
                copied_key.Address = key.Address;
                key = copied_key;

                temp = new byte[salt.SourceArray.Length];
                salt.SourceArray.CopyTo(temp, 0);
                copied_salt = new ArrayPointer<byte>(temp);
                copied_salt.Address = salt.Address;
                salt = copied_salt;

                /* Prepare for the real work.  */
                __sha256_init_ctx(ctx);

                /* Add the key string.  */
                __sha256_process_bytes(key, key_len, ctx);

                /* The last part is the salt string.  This must be at most 16
                   characters and it ends at the first `$' character.  */
                __sha256_process_bytes(salt, salt_len, ctx);


                /* Compute alternate SHA256 sum with input KEY, SALT, and KEY.  The
                   final result will be added to the first context.  */
                __sha256_init_ctx(alt_ctx);

                /* Add key.  */
                __sha256_process_bytes(key, key_len, alt_ctx);

                /* Add salt.  */
                __sha256_process_bytes(salt, salt_len, alt_ctx);

                /* Add key again.  */
                __sha256_process_bytes(key, key_len, alt_ctx);

                /* Now get result of this (32 bytes) and add it to the other
                   context.  */
                __sha256_finish_ctx(alt_ctx, alt_result);

                /* Add for any character in the key one byte of the alternate sum.  */
                for (cnt = key_len; cnt > 32; cnt -= 32)
                    __sha256_process_bytes(alt_result, 32, ctx);
                __sha256_process_bytes(alt_result, cnt, ctx);

                /* Take the binary representation of the length of the key and for every
                   1 add the alternate sum, for every 0 the key.  */
                for (cnt = key_len; cnt > 0; cnt >>= 1)
                    if ((cnt & 1) != 0)
                        __sha256_process_bytes(alt_result, 32, ctx);
                    else
                        __sha256_process_bytes(key, key_len, ctx);

                /* Create intermediate result.  */
                __sha256_finish_ctx(ctx, alt_result);

                /* Start computation of P byte sequence.  */
                __sha256_init_ctx(alt_ctx);

                /* For every character in the password add the entire password.  */
                for (cnt = 0; cnt < key_len; ++cnt)
                    __sha256_process_bytes(key, key_len, alt_ctx);

                /* Finish the digest.  */
                __sha256_finish_ctx(alt_ctx, temp_result);

                /* Create byte sequence P.  */
                cp = p_bytes = new ArrayPointer<byte>(new byte[key_len]);
                for (cnt = key_len; cnt >= 32; cnt -= 32)
                    cp = mempcpy(cp, temp_result, 32);
                memcpy(cp, temp_result, cnt);

                /* Start computation of S byte sequence.  */
                __sha256_init_ctx(alt_ctx);

                /* For every character in the password add the entire password.  */
                for (cnt = 0; cnt < 16 + alt_result[0]; ++cnt)
                    __sha256_process_bytes(salt, salt_len, alt_ctx);

                /* Finish the digest.  */
                __sha256_finish_ctx(alt_ctx, temp_result);

                /* Create byte sequence S.  */
                cp = s_bytes = new ArrayPointer<byte>(new byte[salt_len]);
                for (cnt = salt_len; cnt >= 32; cnt -= 32)
                    cp = mempcpy(cp, temp_result, 32);
                memcpy(cp, temp_result, cnt);

                /* Repeatedly run the collected hash value through SHA256 to burn
                   CPU cycles.  */
                for (cnt = 0; cnt < rounds; ++cnt)
                {
                    /* New context.  */
                    __sha256_init_ctx(ctx);

                    /* Add key or last result.  */
                    if ((cnt & 1) != 0)
                        __sha256_process_bytes(p_bytes, key_len, ctx);
                    else
                        __sha256_process_bytes(alt_result, 32, ctx);

                    /* Add salt for numbers not divisible by 3.  */
                    if (cnt % 3 != 0)
                        __sha256_process_bytes(s_bytes, salt_len, ctx);

                    /* Add key for numbers not divisible by 7.  */
                    if (cnt % 7 != 0)
                        __sha256_process_bytes(p_bytes, key_len, ctx);

                    /* Add key or last result.  */
                    if ((cnt & 1) != 0)
                        __sha256_process_bytes(alt_result, 32, ctx);
                    else
                        __sha256_process_bytes(p_bytes, key_len, ctx);

                    /* Create intermediate result.  */
                    __sha256_finish_ctx(ctx, alt_result);
                }

                /* Now we can construct the result string.  It consists of three
                   parts.  */
                cp = __stpncpy(buffer, sha256_salt_prefix, Math.Max(0, buflen));
                buflen -= strlen(sha256_salt_prefix);

                if (rounds_custom)
                {
                    cp = __stpncpy(cp, sha256_rounds_prefix, Math.Max(0, buflen));
                    buflen -= strlen(sha256_rounds_prefix);

                    char[] temp1 = (rounds.ToString() + "$\0").ToCharArray();
                    byte[] temp2 = new byte[temp1.Length];
                    for (int i = 0; i < temp1.Length; i++) temp2[i] = (byte)temp1[i];
                    ArrayPointer<byte> temp3 = new ArrayPointer<byte>(temp2);

                    cp = __stpncpy(cp, temp3, Math.Max(0, buflen));
                    buflen -= strlen(temp3);
                }

                cp = __stpncpy(cp, salt, Math.Min(Math.Max(0, buflen), salt_len));
                buflen -= Math.Min(Math.Max(0, buflen), salt_len);

                if (buflen > 0)
                {
                    cp.Value = (byte)'$';
                    cp++;
                    buflen--;
                }

                cp = b64_from_24bit(alt_result[0], alt_result[10], alt_result[20], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[21], alt_result[1], alt_result[11], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[12], alt_result[22], alt_result[2], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[3], alt_result[13], alt_result[23], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[24], alt_result[4], alt_result[14], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[15], alt_result[25], alt_result[5], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[6], alt_result[16], alt_result[26], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[27], alt_result[7], alt_result[17], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[18], alt_result[28], alt_result[8], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(alt_result[9], alt_result[19], alt_result[29], 4, cp, buflen, out buflen);
                cp = b64_from_24bit(0, alt_result[31], alt_result[30], 3, cp, buflen, out buflen);
                if (buflen <= 0)
                {
                    throw new IndexOutOfRangeException();
                }
                else
                    cp.Value = 0;       /* Terminate the string.  */

                /* Clear the buffer for the intermediate result so that people
                   attaching to processes or reading core dumps cannot get any
                   information.  We do it in this way to clear correct_words[]
                   inside the SHA256 implementation as well.  */
                __sha256_init_ctx(ctx);
                __sha256_finish_ctx(ctx, alt_result);


                return buffer;
            }

            private static ArrayPointer<byte> memcpy(ArrayPointer<byte> dest, ArrayPointer<byte> src, int n)
            {
                for (int i = 0; i < n; i++)
                {
                    dest[i] = src[i];
                }

                return dest;
            }

            private static ArrayPointer<byte> mempcpy(ArrayPointer<byte> dest, ArrayPointer<byte> src, int n)
            {
                for (int i = 0; i < n; i++)
                {
                    dest[i] = src[i];
                }

                return dest + n;
            }

            private static ulong strtoul(ArrayPointer<byte> str, out ArrayPointer<byte> endptr, int numberbase)
            {
                if (numberbase != 10) throw new ArgumentOutOfRangeException("numberbase");

                string num = "";
                for (int i = 0; str.Value >= '0' && str.Value <= '9'; i++, str++)
                {
                    num += (char)str.Value;
                }

                endptr = str;

                ulong value = 0;
                ulong.TryParse(num, out value);

                return value;
            }

            private static ArrayPointer<byte> b64_from_24bit(uint B2, uint B1, uint B0, int N, ArrayPointer<byte> cp, int buflen, out int buflen_out)
            {
                uint w = ((B2) << 16) | ((B1) << 8) | (B0);
                int n = (N);
                while (n-- > 0 && buflen > 0)
                {
                    cp.Value = (byte)b64t[w & 0x3f];
                    cp++;
                    buflen--;
                    w >>= 6;
                }
                buflen_out = buflen;
                return cp;
            }

            private static void __md5_init_ctx(md5_ctx ctx)
            {
                ctx.stream = new MemoryStream();
            }

            private static void __md5_process_bytes(ArrayPointer<byte> buffer, int count, md5_ctx ctx)
            {
                ctx.stream.Write(buffer.SourceArray, buffer.Address, count);
            }

            private static void __md5_finish_ctx(md5_ctx ctx, ArrayPointer<byte> buffer)
            {
                byte[] temp = new byte[ctx.stream.Length];
                ctx.stream.Seek(0, SeekOrigin.Begin);
                ctx.stream.Read(temp, 0, temp.Length);

                temp = MD5.Create().ComputeHash(temp);

                for (int i = 0; i < temp.Length; i++, buffer++)
                {
                    buffer.Value = temp[i];
                }
            }

            private static void __sha256_init_ctx(sha256_ctx ctx)
            {
                ctx.stream = new MemoryStream();
            }

            private static void __sha256_process_bytes(ArrayPointer<byte> buffer, int count, sha256_ctx ctx)
            {
                ctx.stream.Write(buffer.SourceArray, buffer.Address, count);
            }

            private static void __sha256_finish_ctx(sha256_ctx ctx, ArrayPointer<byte> buffer)
            {
                byte[] temp = new byte[ctx.stream.Length];
                ctx.stream.Seek(0, SeekOrigin.Begin);
                ctx.stream.Read(temp, 0, temp.Length);

                temp = SHA256.Create().ComputeHash(temp);

                for (int i = 0; i < temp.Length; i++, buffer++)
                {
                    buffer.Value = temp[i];
                }
            }

            private static ArrayPointer<byte> __stpncpy(ArrayPointer<byte> buffer, ArrayPointer<byte> source, int max)
            {
                int i;

                for (i = 0; i < max; i++, buffer++)
                {
                    if (source[i] == 0)
                    {
                        break;
                    }

                    buffer.Value = source[i];
                }

                return buffer;
            }

            private static string ExtractString(ArrayPointer<byte> str)
            {
                StringBuilder sb = new StringBuilder(strlen(str));

                for (int i = 0; ; i++)
                {
                    if (str[i] == 0)
                        break;
                    sb.Append((char)str[i]);
                }

                return sb.ToString();
            }
        }
    }

