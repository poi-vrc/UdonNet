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
            EditorSceneManager.OpenScene("Assets/Scenes/TestScene_BufferToolkit.unity");
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
    }
}
