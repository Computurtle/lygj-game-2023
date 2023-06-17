//	Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.AudioManagement {
	public static class SavWav {

		const int _Header_Size = 44;

		public static bool Save( string Filename, AudioClip Clip ) {
			if (!Filename.ToLower().EndsWith(".wav")) {
				Filename += ".wav";
			}

			string Filepath = Path.Combine(Application.persistentDataPath, Filename);

			Debug.Log(Filepath);

			// Make sure directory exists if user is saving to sub dir.
			Directory.CreateDirectory(Path.GetDirectoryName(Filepath)!);

			using FileStream FileStream = CreateEmpty(Filepath);
			ConvertAndWrite(FileStream, Clip);

			WriteHeader(FileStream, Clip);

			return true; // TODO: return false if there's a failure saving the file
		}

		public static AudioClip TrimSilence( AudioClip Clip, float Min ) {
			float[] Samples = new float[Clip.samples];

			Clip.GetData(Samples, 0);

			return TrimSilence(new(Samples), Min, Clip.channels, Clip.frequency);
		}

		public static AudioClip TrimSilence( List<float> Samples, float Min, int Channels, int Hz ) => TrimSilence(Samples, Min, Channels, Hz, false, false);

		public static AudioClip TrimSilence( List<float> Samples, float Min, int Channels, int Hz, bool _3D, bool Stream ) {
			int I;

			for (I = 0; I < Samples.Count; I++) {
				if (Mathf.Abs(Samples[I]) > Min) {
					break;
				}
			}

			Samples.RemoveRange(0, I);

			for (I = Samples.Count - 1; I > 0; I--) {
				if (Mathf.Abs(Samples[I]) > Min) {
					break;
				}
			}

			Samples.RemoveRange(I, Samples.Count - I);

			#pragma warning disable CS0618
			AudioClip? Clip = AudioClip.Create("TempClip", Samples.Count, Channels, Hz, _3D, Stream);
			#pragma warning restore CS0618

			Clip.SetData(Samples.ToArray(), 0);

			return Clip;
		}

		static FileStream CreateEmpty( string Filepath ) {
			FileStream FileStream = new(Filepath, FileMode.Create);
			const byte EmptyByte  = new();

			for (int I = 0; I < _Header_Size; I++) //preparing the header
			{
				FileStream.WriteByte(EmptyByte);
			}

			return FileStream;
		}

		static void ConvertAndWrite( FileStream FileStream, AudioClip Clip ) {

			float[] Samples = new float[Clip.samples];

			Clip.GetData(Samples, 0);

			short[] INTData = new short[Samples.Length];
			//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

			byte[] BytesData = new byte[Samples.Length * 2];
			//bytesData array is twice the size of
			//dataSource array because a float converted in Int16 is 2 bytes.

			const int RescaleFactor = 32767; //to convert float to Int16

			for (int I = 0; I < Samples.Length; I++) {
				INTData[I] = (short)(Samples[I] * RescaleFactor);
				byte[] ByteArr = new byte[2];
				ByteArr = BitConverter.GetBytes(INTData[I]);
				ByteArr.CopyTo(BytesData, I * 2);
			}

			FileStream.Write(BytesData, 0, BytesData.Length);
		}

		static void WriteHeader( FileStream FileStream, AudioClip Clip ) {

			int Hz       = Clip.frequency;
			int Channels = Clip.channels;
			int Samples  = Clip.samples;

			FileStream.Seek(0, SeekOrigin.Begin);

			byte[] Riff = Encoding.UTF8.GetBytes("RIFF");
			FileStream.Write(Riff, 0, 4);

			byte[] ChunkSize = BitConverter.GetBytes(FileStream.Length - 8);
			FileStream.Write(ChunkSize, 0, 4);

			byte[] Wave = Encoding.UTF8.GetBytes("WAVE");
			FileStream.Write(Wave, 0, 4);

			byte[] Fmt = Encoding.UTF8.GetBytes("fmt ");
			FileStream.Write(Fmt, 0, 4);

			byte[] SubChunk1 = BitConverter.GetBytes(16);
			FileStream.Write(SubChunk1, 0, 4);

			const ushort One = 1;

			byte[] AudioFormat = BitConverter.GetBytes(One);
			FileStream.Write(AudioFormat, 0, 2);

			byte[] NumChannels = BitConverter.GetBytes(Channels);
			FileStream.Write(NumChannels, 0, 2);

			byte[] SampleRate = BitConverter.GetBytes(Hz);
			FileStream.Write(SampleRate, 0, 4);

			byte[] ByteRate = BitConverter.GetBytes(Hz * Channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
			FileStream.Write(ByteRate, 0, 4);

			ushort BlockAlign = (ushort)(Channels * 2);
			FileStream.Write(BitConverter.GetBytes(BlockAlign), 0, 2);

			const ushort Bps           = 16;
			byte[]       BitsPerSample = BitConverter.GetBytes(Bps);
			FileStream.Write(BitsPerSample, 0, 2);

			byte[] Datastring = Encoding.UTF8.GetBytes("data");
			FileStream.Write(Datastring, 0, 4);

			byte[] SubChunk2 = BitConverter.GetBytes(Samples * Channels * 2);
			FileStream.Write(SubChunk2, 0, 4);

			// FileStream.Close();
		}
	}
}
