using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UdonNet;
using UdonSharp;
using VRC.Udon; 
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace Tests
{
    public class BufferToolkitTestScript
    {
        private BufferToolkit PrepareBufferToolkit()
        {
            if (EditorSceneManager.GetActiveScene().name != "TestScene_BufferToolkit")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/TestScene_BufferToolkit.unity");
            }
            return (BufferToolkit)GameObject.Find("BufferToolkit")?.GetComponent<UdonSharpBehaviour>();
        }

        [Test]
        public void ReadWriteBoolean()
        {
            BufferToolkit bufferToolkit = PrepareBufferToolkit();

            //Generate a random index for testing
            int index = Mathf.RoundToInt(Random.Range(0, 127));
            byte[] array = new byte[128];
            byte[] expectedArray = new byte[128];
            expectedArray[index] = byte.MaxValue;

            //Test for write
            int c = bufferToolkit.WriteBoolean(array, index, true);
            Assert.AreEqual(1, c, "The expected byte write count for boolean must be 1.");
            Assert.AreEqual(expectedArray, array, "The byte array should have a byte.MaxValue at written index \"" + index + "\"");

            //Test for read
            bool result = bufferToolkit.ReadBoolean(array, index);
            Assert.AreEqual(true, result, "The expected boolean read should be \"true\"");
        }

        [Test]
        public void ReadWriteByte()
        {
            BufferToolkit bufferToolkit = PrepareBufferToolkit();

            //Generate a random index for testing
            int index = Mathf.RoundToInt(Random.Range(0, 127));
            byte val = (byte) Mathf.RoundToInt(Random.Range(0, 255));
            byte[] array = new byte[128];
            byte[] expectedArray = new byte[128];
            expectedArray[index] = val;

            //Test for write
            int c = bufferToolkit.WriteByte(array, index, val);
            Assert.AreEqual(1, c, "The expected byte write count for byte must be 1.");
            Assert.AreEqual(expectedArray, array, "The byte array should have a \"" + val + "\" at written index \"" + index + "\"");

            //Test for read
            byte result = bufferToolkit.ReadByte(array, index);
            Assert.AreEqual(val, result, "The expected byte read should be \"" + val + "\"");
        }

        [Test]
        public void ReadWriteBytes()
        {
            BufferToolkit bufferToolkit = PrepareBufferToolkit();

            //Generate random values for testing
            int start = Mathf.RoundToInt(Random.Range(0, 42));
            int end = Mathf.RoundToInt(Random.Range(85, 128));
            int count = end - start;

            byte[] bytesToWrite = new byte[count];
            byte[] expectedArray = new byte[128];
            
            for (int i = 0; i < count; i++)
            {
                byte val = (byte)Mathf.RoundToInt(Random.Range(0, 255));
                expectedArray[start + i] = val;
                bytesToWrite[i] = val;
            }

            byte[] array = new byte[128];
            //Test for write
            int c = bufferToolkit.WriteBytes(array, start, bytesToWrite, 0, count);
            Assert.AreEqual(count, c, "The expected byte write count for byte must be \"" + count + "\".");
            Assert.AreEqual(expectedArray, array, "The byte array should be the same as the provided byte array.");

            //Test for read
            byte[] result = bufferToolkit.ReadBytes(array, start, count);
            Assert.AreEqual(bytesToWrite, result, "The expected byte read should be the same as the provided byte array.");
        }
    }
}
