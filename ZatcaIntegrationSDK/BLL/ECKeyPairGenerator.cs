﻿using System;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Math.EC.Multiplier;
using Org.BouncyCastle.Security;

namespace ZatcaIntegrationSDK
{
    public class ECKeyPairGenerator : IAsymmetricCipherKeyPairGenerator
    {
        private readonly string algorithm;

        private ECDomainParameters parameters;
        private DerObjectIdentifier publicKeyParamSet;
        private SecureRandom random;

        public ECKeyPairGenerator()
            : this("EC")
        {
        }

        public ECKeyPairGenerator(
            string algorithm)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");

            this.algorithm = algorithm;
        }

        /// <summary>
        /// Init method of the class 
        /// CEBA.Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator updated as below
        /// </summary>
        /// <param name="parameters"></param>
        public void Init(KeyGenerationParameters parameters)
        {
            X9ECParameters ecps = FindECCurveByOid(SecObjectIdentifiers.SecP256k1);
            this.publicKeyParamSet = SecObjectIdentifiers.SecP256k1;


            this.parameters = new ECDomainParameters(
                ecps.Curve, ecps.G, ecps.N, ecps.H, ecps.GetSeed());


            this.random = parameters.Random;

            if (this.random == null)
            {
                this.random = new SecureRandom();
            }
        }

        /**
         * Given the domain parameters this routine generates an EC key
         * pair in accordance with X9.62 section 5.2.1 pages 26, 27.
         */
        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            BigInteger n = parameters.N;
            BigInteger d;
            int minWeight = n.BitLength >> 2;

            for (; ; )
            {
                d = new BigInteger(n.BitLength, random);

                if (d.CompareTo(BigInteger.Two) < 0 || d.CompareTo(n) >= 0)
                    continue;

                if (WNafUtilities.GetNafWeight(d) < minWeight)
                    continue;

                break;
            }

            ECPoint q = CreateBasePointMultiplier().Multiply(parameters.G, d);

            if (publicKeyParamSet != null)
            {
                return new AsymmetricCipherKeyPair(
                    new ECPublicKeyParameters(algorithm, q, publicKeyParamSet),
                    new ECPrivateKeyParameters(algorithm, d, publicKeyParamSet));
            }

            return new AsymmetricCipherKeyPair(
                new ECPublicKeyParameters(algorithm, q, parameters),
                new ECPrivateKeyParameters(algorithm, d, parameters));
        }

        protected virtual ECMultiplier CreateBasePointMultiplier()
        {
            return new FixedPointCombMultiplier();
        }

        internal static X9ECParameters FindECCurveByOid(DerObjectIdentifier oid)
        {
            // TODO ECGost3410NamedCurves support (returns ECDomainParameters though)

            X9ECParameters ecP = CustomNamedCurves.GetByOid(oid);
            if (ecP == null)
            {
                ecP = ECNamedCurveTable.GetByOid(oid);
            }
            return ecP;
        }

        internal static ECPublicKeyParameters GetCorrespondingPublicKey(
            ECPrivateKeyParameters privKey)
        {
            ECDomainParameters ec = privKey.Parameters;
            ECPoint q = new FixedPointCombMultiplier().Multiply(ec.G, privKey.D);

            if (privKey.PublicKeyParamSet != null)
            {
                return new ECPublicKeyParameters(privKey.AlgorithmName, q, privKey.PublicKeyParamSet);
            }

            return new ECPublicKeyParameters(privKey.AlgorithmName, q, ec);
        }
    }
}
