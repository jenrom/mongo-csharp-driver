﻿/* Copyright 2010 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient.Internal {
    public abstract class MongoRequestMessage : MongoMessage {
        #region protected fields
        protected MongoCollection collection; // null if subclass is not a collection related message (e.g. KillCursors)
        protected MemoryStream memoryStream; // null until WriteTo has been called
        protected long messageStart; // start position in stream for backpatching messageLength
        #endregion

        #region constructors
        protected MongoRequestMessage(
            MessageOpcode opcode,
            MongoCollection collection
        )
            : base(opcode) {
            this.collection = collection;
        }
        #endregion

        #region public methods
        public MemoryStream AsMemoryStream() {
            if (memoryStream == null) {
                memoryStream = new MemoryStream();
                WriteTo(memoryStream);
            }
            return memoryStream;
        }

        public void WriteTo(
            MemoryStream memoryStream
        ) {
            this.memoryStream = memoryStream;
            messageStart = memoryStream.Position;
            var binaryWriter = new BinaryWriter(memoryStream);
            WriteMessageHeaderTo(binaryWriter);
            WriteBodyTo(binaryWriter);
            BackpatchMessageLength(binaryWriter);
        }
        #endregion

        #region protected methods
        protected void BackpatchMessageLength(
            BinaryWriter binaryWriter
        ) {
            long currentPosition = binaryWriter.BaseStream.Position;
            messageLength = (int) (currentPosition - messageStart);
            binaryWriter.BaseStream.Seek(messageStart, SeekOrigin.Begin);
            binaryWriter.Write(messageLength);
            binaryWriter.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
        }

        protected abstract void WriteBodyTo(
            BinaryWriter binaryWriter
        );

        protected void WriteCStringTo(
            BinaryWriter binaryWriter,
            string value
        ) {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            if (utf8Bytes.Contains((byte) 0)) {
                throw new MongoException("A cstring cannot contain 0x00");
            }
            binaryWriter.Write(utf8Bytes);
            binaryWriter.Write((byte) 0);
        }
        #endregion
    }
}