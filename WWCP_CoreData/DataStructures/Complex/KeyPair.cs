﻿/*
 * Copyright (c) 2014-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP OCPP <https://github.com/OpenChargingCloud/WWCP_OCPP>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Pkcs;

#endregion

namespace cloud.charging.open.protocols.WWCP
{

    /// <summary>
    /// An OCPP CSE asymmetric cryptographic key pair.
    /// </summary>
    public class KeyPair : ACustomData,
                           IEquatable<KeyPair>
    {

        #region Properties

        /// <summary>
        /// The cryptographic public key.
        /// </summary>
        [Mandatory]
        public   Byte[]                   PublicKeyBytes                { get; }

        /// <summary>
        /// The optional cryptographic private key.
        /// </summary>
        [Optional]
        public   Byte[]                   PrivateKeyBytes               { get; }

        /// <summary>
        /// The optional cryptographic algorithm of the keys. Default is 'secp256r1'.
        /// </summary>
        [Optional]
        public   CryptoAlgorithm?         Algorithm             { get; }

        /// <summary>
        /// The optional serialization of the cryptographic keys. Default is 'raw'.
        /// </summary>
        [Optional]
        public   CryptoSerialization?     Serialization         { get; }

        /// <summary>
        /// The optional encoding of the cryptographic keys. Default is 'base64'.
        /// </summary>
        [Optional]
        public   CryptoEncoding?          Encoding              { get; }


        public   X9ECParameters           ECParameters          { get; }

        public   ECDomainParameters       ECDomainParameters    { get; }


        internal ECPrivateKeyParameters?  PrivateKey            { get; }

        internal ECPublicKeyParameters    PublicKey             { get; }


        public Byte[] PublicKeyASN1
            => SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(PublicKey).ToAsn1Object().GetEncoded();

        public Byte[] PrivateKeyASN1
            => PrivateKeyInfoFactory.CreatePrivateKeyInfo(PrivateKey).ToAsn1Object().GetEncoded();

        #endregion

        #region Constructor(s)

        #region KeyPair(...)

        /// <summary>
        /// Create a new OCPP CSE asymmetric cryptographic key pair.
        /// </summary>
        /// <param name="Private">The private key.</param>
        /// <param name="Public">The public key.</param>
        /// <param name="Algorithm">The optional cryptographic algorithm of the keys. Default is 'secp256r1'.</param>
        /// <param name="Serialization">The optional serialization of the cryptographic keys. Default is 'raw'.</param>
        /// <param name="Encoding">The optional encoding of the cryptographic keys. Default is 'base64'.</param>
        /// <param name="CustomData">An optional custom data object to allow to store any kind of customer specific data.</param>
        public KeyPair(Byte[]                Public,
                       Byte[]?               Private         = null,
                       CryptoAlgorithm?      Algorithm       = null,
                       CryptoSerialization?  Serialization   = null,
                       CryptoEncoding?       Encoding        = null,
                       CustomData?           CustomData      = null)

            : base(CustomData)

        {

            this.PublicKeyBytes              = Public;
            this.PrivateKeyBytes             = Private ?? [];
            this.Algorithm           = Algorithm;
            this.Serialization       = Serialization;
            this.Encoding            = Encoding;

            this.ECParameters        = ECNamedCurveTable.GetByName(this.Algorithm?.ToString() ?? "secp256r1");

            if (this.ECParameters is null)
                throw new ArgumentException("The given cryptographic algorithm is unknown!", nameof(Algorithm));

            this.ECDomainParameters  = new ECDomainParameters(
                                           ECParameters.Curve,
                                           ECParameters.G,
                                           ECParameters.N,
                                           ECParameters.H,
                                           ECParameters.GetSeed()
                                       );

            #region Try to parse the public key

            try
            {

                this.PublicKey       = new ECPublicKeyParameters(
                                           "ECDSA",
                                           ECParameters.Curve.DecodePoint(this.PublicKeyBytes),
                                           ECDomainParameters
                                       );

            }
            catch (Exception e)
            {
                throw new ArgumentException("The given public key is invalid!", nameof(Public), e);
            }

            #endregion

            #region Try to parse the private key

            if (this.PrivateKeyBytes.Length > 0)
            {

                try
                {

                    this.PrivateKey = new ECPrivateKeyParameters(
                                          new BigInteger(this.PrivateKeyBytes),
                                          ECDomainParameters
                                      );

                }
                catch (Exception e)
                {
                    throw new ArgumentException("The given private key is invalid!", nameof(Private), e);
                }

            }

            #endregion


            unchecked
            {

                hashCode = this.PublicKeyBytes.       GetHashCode()       * 13 ^
                           this.PrivateKeyBytes.      GetHashCode()       * 11 ^
                          (this.Algorithm?.   GetHashCode() ?? 0) *  7 ^
                           this.Serialization.GetHashCode()       *  5 ^
                          (this.Encoding?.    GetHashCode() ?? 0) *  3 ^
                           base.              GetHashCode();

            }

        }

        #endregion

        #region KeyPair(...)

        /// <summary>
        /// Create a new OCPP CSE asymmetric cryptographic key pair.
        /// </summary>
        /// <param name="PrivateKeyBytes">The private key.</param>
        /// <param name="PublicKeyBytes">The public key.</param>
        /// <param name="Algorithm">The optional cryptographic algorithm of the keys. Default is 'secp256r1'.</param>
        /// <param name="Serialization">The optional serialization of the cryptographic keys. Default is 'raw'.</param>
        /// <param name="Encoding">The optional encoding of the cryptographic keys. Default is 'base64'.</param>
        /// <param name="CustomData">An optional custom data object to allow to store any kind of customer specific data.</param>
        public KeyPair(ECPublicKeyParameters    PublicKey,
                       Byte[]                   PublicKeyBytes,
                       ECPrivateKeyParameters?  PrivateKey        = null,
                       Byte[]?                  PrivateKeyBytes   = null,
                       CryptoAlgorithm?         Algorithm         = null,
                       CryptoSerialization?     Serialization     = null,
                       CryptoEncoding?          Encoding          = null,
                       CustomData?              CustomData        = null)

            : base(CustomData)

        {

            this.PublicKey           = PublicKey;
            this.PublicKeyBytes      = PublicKeyBytes;
            this.PrivateKey          = PrivateKey;
            this.PrivateKeyBytes     = PrivateKeyBytes ?? [];
            this.Algorithm           = Algorithm;
            this.Serialization       = Serialization;
            this.Encoding            = Encoding;

        


            unchecked
            {

                hashCode = this.PublicKeyBytes. GetHashCode()       * 13 ^
                           this.PrivateKeyBytes.GetHashCode()       * 11 ^
                          (this.Algorithm?.     GetHashCode() ?? 0) *  7 ^
                           this.Serialization.  GetHashCode()       *  5 ^
                          (this.Encoding?.      GetHashCode() ?? 0) *  3 ^
                           base.                GetHashCode();

            }

        }

        #endregion

        #endregion


        #region Documentation

        // tba.

        #endregion

        #region (static) GenerateKeys(Algorithm = secp256r1)

        public static KeyPair? GenerateKeys(String? Algorithm = "secp256r1")
        {

            var ecParameters  = ECNamedCurveTable. GetByName(Algorithm ?? "secp256r1");

            if (ecParameters is null)
                return null;

            var g             = GeneratorUtilities.GetKeyPairGenerator("ECDH");

            g.Init(new ECKeyGenerationParameters(
                       new ECDomainParameters(
                           ecParameters.Curve,
                           ecParameters.G,
                           ecParameters.N,
                           ecParameters.H,
                           ecParameters.GetSeed()
                       ),
                       new SecureRandom()
                   ));

            var keyPair = g.GenerateKeyPair();

            return new KeyPair(
                       (keyPair.Public  as ECPublicKeyParameters)?. Q.GetEncoded() ?? [],
                       (keyPair.Private as ECPrivateKeyParameters)?.D.ToByteArray()
                   );

        }

        #endregion

        #region (static) Parse       (JSON, CustomKeyPairParser = null)

        /// <summary>
        /// Parse the given JSON representation of a cryptographic key pair.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomKeyPairParser">An optional delegate to parse custom cryptographic key pairs.</param>
        public static KeyPair Parse(JObject                                JSON,
                                    CustomJObjectParserDelegate<KeyPair>?  CustomKeyPairParser   = null)
        {

            if (TryParse(JSON,
                         out var keyPair,
                         out var errorResponse,
                         CustomKeyPairParser) &&
                keyPair is not null)
            {
                return keyPair;
            }

            throw new ArgumentException("The given JSON representation of a key pair is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse    (JSON, out KeyPair, out ErrorResponse, CustomKeyPairParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a key pair.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="KeyPair">The parsed connector type.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                            JSON,
                                       [NotNullWhen(true)]  out KeyPair?  KeyPair,
                                       [NotNullWhen(false)] out String?   ErrorResponse)

            => TryParse(JSON,
                        out KeyPair,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a key pair.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="KeyPair">The parsed connector type.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomKeyPairParser">An optional delegate to parse custom key pairs.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       [NotNullWhen(true)]  out KeyPair?      KeyPair,
                                       [NotNullWhen(false)] out String?       ErrorResponse,
                                       CustomJObjectParserDelegate<KeyPair>?  CustomKeyPairParser   = null)
        {

            try
            {

                KeyPair = default;

                #region Private          [mandatory]

                if (!JSON.ParseMandatoryText("private",
                                             "private key",
                                             out String PrivateText,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Public           [mandatory]

                if (!JSON.ParseMandatoryText("public",
                                             "public key",
                                             out String PublicText,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Algorithm        [optional]

                if (JSON.ParseOptional("algorithm",
                                       "crypto algorithm",
                                       CryptoAlgorithm.TryParse,
                                       out CryptoAlgorithm? Algorithm,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Serialization    [optional]

                if (JSON.ParseOptional("serialization",
                                       "crypto serialization",
                                       CryptoSerialization.TryParse,
                                       out CryptoSerialization? Serialization,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Encoding         [optional]

                if (JSON.ParseOptional("encoding",
                                       "crypto encoding",
                                       CryptoEncoding.TryParse,
                                       out CryptoEncoding? Encoding,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                var Private  = PrivateText.FromBase64();
                var Public   = PublicText. FromBase64();

                #endregion

                #region CustomData       [optional]

                if (JSON.ParseOptionalJSON("customData",
                                           "custom data",
                                           WWCP.CustomData.TryParse,
                                           out CustomData CustomData,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                KeyPair = new KeyPair(
                              Private,
                              Public,
                              Algorithm,
                              Serialization,
                              Encoding,
                              CustomData
                          );

                if (CustomKeyPairParser is not null)
                    KeyPair = CustomKeyPairParser(JSON,
                                                  KeyPair);

                return true;

            }
            catch (Exception e)
            {
                KeyPair        = default;
                ErrorResponse  = "The given JSON representation of a key pair is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomKeyPairSerializer = null, CustomCustomDataSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomKeyPairSerializer">A delegate to serialize cryptographic key pairs.</param>
        /// <param name="CustomCustomDataSerializer">A delegate to serialize CustomData objects.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<KeyPair>?     CustomKeyPairSerializer      = null,
                              CustomJObjectSerializerDelegate<CustomData>?  CustomCustomDataSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("private",         PrivateKeyBytes),
                                 new JProperty("public",          PublicKeyBytes),

                           Algorithm.    HasValue && Algorithm.    Value != CryptoAlgorithm.secp256r1
                               ? new JProperty("algorithm",       Algorithm.Value.ToString())
                               : null,

                           Serialization.HasValue && Serialization.Value != CryptoSerialization.raw
                               ? new JProperty("serialization",   Serialization)
                               : null,

                           Encoding.     HasValue && Encoding.     Value != CryptoEncoding.BASE64
                               ? new JProperty("encoding",        Encoding. Value.ToString())
                               : null,

                           CustomData is not null
                               ? new JProperty("customData",      CustomData.ToJSON(CustomCustomDataSerializer))
                               : null

                       );

            return CustomKeyPairSerializer is not null
                       ? CustomKeyPairSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this object.
        /// </summary>
        public KeyPair Clone()

            => new (

                   (Byte[]) PrivateKeyBytes.Clone(),
                   (Byte[]) PublicKeyBytes. Clone(),

                   Algorithm?.    Clone,
                   Serialization?.Clone,
                   Encoding?.     Clone,

                   CustomData

               );

        #endregion


        #region (static) ParsePrivateKey    (PrivateKey,              Serialization = "base64", Algorithm = "secp256r1")

        /// <summary>
        /// Parse the given private key and calculate its public key.
        /// </summary>
        /// <param name="PrivateKey">A text representation of a private key.</param>
        /// <param name="Serialization">The optional serialization of the cryptographic keys. [default: base64]</param>
        /// <param name="Algorithm">The optional cryptographic algorithm of the private key. [default: secp256r1]</param>
        public static KeyPair? ParsePrivateKey(String   PrivateKey,
                                               String?  Serialization   = "base64",
                                               String?  Algorithm       = "secp256r1")
        {

            if (TryParsePrivateKey(PrivateKey,
                                   out var keyPair,
                                   Serialization,
                                   Algorithm))
            {
                return keyPair;
            }

            return null;

        }

        #endregion

        #region (static) TryParsePrivateKey (PrivateKey, out KeyPair, Serialization = "base64", Algorithm = "secp256r1")

        /// <summary>
        /// Try to parse the given private key and calculate its public key.
        /// </summary>
        /// <param name="PrivateKey">A text representation of a private key.</param>
        /// <param name="KeyPair">The parsed key pair.</param>
        /// <param name="Serialization">The optional serialization of the cryptographic keys. [default: base64]</param>
        /// <param name="Algorithm">The optional cryptographic algorithm of the private key. [default: secp256r1]</param>
        public static Boolean TryParsePrivateKey(String        PrivateKey,
                                                 out KeyPair?  KeyPair,
                                                 String?       Serialization   = "base64",
                                                 String?       Algorithm       = "secp256r1")
        {

            KeyPair = null;

            Byte[]? privateKeyBytes;
            try
            {

                switch (Serialization)
                {

                    case "base64":
                        privateKeyBytes = PrivateKey.FromBase64();
                        break;

                    default:
                        return false;

                }

            }
            catch
            {
                return false;
            }

            var ecParameters      = ECNamedCurveTable.GetByName(Algorithm ?? "secp256r1")
                                      ?? throw new ArgumentException($"Invalid algorithm: {Algorithm}", nameof(Algorithm));

            var d                 = new BigInteger(
                                        1,
                                        privateKeyBytes
                                    );

            var privateKeyParams  = new ECPrivateKeyParameters(
                                        d,
                                        new ECDomainParameters(
                                            ecParameters.Curve,
                                            ecParameters.G,
                                            ecParameters.N,
                                            ecParameters.H,
                                            ecParameters.GetSeed()
                                        )
                                    );

            var q                 = ecParameters.G.Multiply(privateKeyParams.D);

            KeyPair = new KeyPair(
                          q.GetEncoded(false),
                          privateKeyParams.D.ToByteArray()
                      );

            return true;

        }

        #endregion


        #region (static) ParsePublicKey     (PublicKey,              Serialization = "base64", Algorithm = "secp256r1")

        /// <summary>
        /// Parse the given public key.
        /// </summary>
        /// <param name="PublicKey">A text representation of a public key.</param>
        /// <param name="Serialization">The optional serialization of the cryptographic keys. [default: base64]</param>
        /// <param name="Algorithm">The optional cryptographic algorithm of the public key. [default: secp256r1]</param>
        public static KeyPair? ParsePublicKey(String   PublicKey,
                                              String?  Serialization   = "base64",
                                              String?  Algorithm       = "secp256r1")
        {

            if (TryParsePublicKey(PublicKey,
                                  out var keyPair,
                                  Serialization,
                                  Algorithm))
            {
                return keyPair;
            }

            return null;

        }

        #endregion

        #region (static) TryParsePublicKey  (PublicKey, out KeyPair, Serialization = "base64", Algorithm = "secp256r1")

        /// <summary>
        /// Try to parse the given public key.
        /// </summary>
        /// <param name="PublicKey">A text representation of a public key.</param>
        /// <param name="KeyPair">The parsed key pair.</param>
        /// <param name="Serialization">The optional serialization of the cryptographic keys. [default: base64]</param>
        /// <param name="Algorithm">The optional cryptographic algorithm of the public key. [default: secp256r1]</param>
        public static Boolean TryParsePublicKey(String        PublicKey,
                                                out KeyPair?  KeyPair,
                                                String?       Serialization   = "base64",
                                                String?       Algorithm       = "secp256r1")
        {

            KeyPair = null;

            Byte[]? publicKeyBytes;
            try
            {

                switch (Serialization)
                {

                    case "base64":
                        publicKeyBytes = PublicKey.FromBase64();
                        break;

                    default:
                        return false;

                }

            }
            catch
            {
                return false;
            }


            var ecParameters = ECNamedCurveTable.GetByName(Algorithm ?? "secp256r1");

            if (ecParameters is null)
                return false;
                //throw new ArgumentException("The given cryptographic algorithm is unknown!", nameof(Algorithm));

            var ecDomainParameters  = new ECDomainParameters(
                                          ecParameters.Curve,
                                          ecParameters.G,
                                          ecParameters.N,
                                          ecParameters.H,
                                          ecParameters.GetSeed()
                                      );

            #region Try to parse the public key

            ECPublicKeyParameters? publicKey = null;

            try
            {

                publicKey = new ECPublicKeyParameters(
                                "ECDSA",
                                ecParameters.Curve.DecodePoint(publicKeyBytes),
                                ecDomainParameters
                            );

            }
            catch (Exception e)
            {
                return false;
                //throw new ArgumentException("The given public key is invalid!", nameof(PublicKeyBytes), e);
            }

            #endregion

            #region Try to parse the private key

            //if (this.PrivateKeyBytes.Length > 0)
            //{

            //    try
            //    {

            //        this.PrivateKey = new ECPrivateKeyParameters(
            //                              new BigInteger(this.PrivateKeyBytes),
            //                              ECDomainParameters
            //                          );

            //    }
            //    catch (Exception e)
            //    {
            //        throw new ArgumentException("The given private key is invalid!", nameof(PrivateKeyBytes), e);
            //    }

            //}

            #endregion


            KeyPair = new KeyPair(
                          publicKey,
                          publicKeyBytes
                          //q.GetEncoded(false),
                          //privateKeyParams.D.ToByteArray()
                      );

            return true;

        }

        #endregion



        #region Operator overloading

        #region Operator == (KeyPair1, KeyPair2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="KeyPair1">A key pair.</param>
        /// <param name="KeyPair2">Another key pair.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (KeyPair? KeyPair1,
                                           KeyPair? KeyPair2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(KeyPair1, KeyPair2))
                return true;

            // If one is null, but not both, return false.
            if (KeyPair1 is null || KeyPair2 is null)
                return false;

            return KeyPair1.Equals(KeyPair2);

        }

        #endregion

        #region Operator != (KeyPair1, KeyPair2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="KeyPair1">A key pair.</param>
        /// <param name="KeyPair2">Another key pair.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (KeyPair? KeyPair1,
                                           KeyPair? KeyPair2)

            => !(KeyPair1 == KeyPair2);

        #endregion

        #endregion

        #region IEquatable<KeyPair> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two key pairs for equality.
        /// </summary>
        /// <param name="Object">A key pair to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is KeyPair keyPair &&
                   Equals(keyPair);

        #endregion

        #region Equals(KeyPair)

        /// <summary>
        /// Compares two key pairs for equality.
        /// </summary>
        /// <param name="KeyPair">A key pair to compare with.</param>
        public Boolean Equals(KeyPair? KeyPair)

            => KeyPair is not null &&

               PrivateKeyBytes.SequenceEqual(KeyPair.PrivateKeyBytes) &&
               PublicKeyBytes. SequenceEqual(KeyPair.PublicKeyBytes)  &&

            ((!Algorithm.    HasValue && !KeyPair.Algorithm.    HasValue) ||
              (Algorithm.    HasValue &&  KeyPair.Algorithm.    HasValue && Algorithm.    Value.Equals(KeyPair.Algorithm.    Value))) &&

            ((!Serialization.HasValue && !KeyPair.Serialization.HasValue) ||
              (Serialization.HasValue &&  KeyPair.Serialization.HasValue && Serialization.Value.Equals(KeyPair.Serialization.Value))) &&

            ((!Encoding.     HasValue && !KeyPair.Encoding.     HasValue) ||
              (Encoding.     HasValue &&  KeyPair.Encoding.     HasValue && Encoding.     Value.Equals(KeyPair.Encoding.     Value))) &&

               base.  Equals(KeyPair);

        #endregion

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   PrivateKeyBytes is not null
                       ? $"private: {PrivateKeyBytes.ToBase64()}, "
                       : "",

                   $"public: {PublicKeyBytes.ToBase64()}"

               );

        #endregion

    }

}
