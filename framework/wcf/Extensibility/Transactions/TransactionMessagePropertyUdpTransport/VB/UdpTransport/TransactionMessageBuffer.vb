﻿'  Copyright (c) Microsoft Corporation. All rights reserved.

Imports System
Imports System.IO
Imports System.Transactions

Namespace Microsoft.ServiceModel.Samples
	Friend NotInheritable Class TransactionMessageBuffer
		''' <summary>
		''' Generates a buffer in which it puts the lenght of transaction's propagation token, 
		''' the propagation token, and lastly the message bytes. If the transaction propagation token 
		''' is null, we insert 4 null bytes at the beginning of the buffer.       
		''' </summary>
		Private Sub New()
		End Sub
        Public Shared Function WriteTransactionMessageBuffer(ByVal txPropToken() As Byte,
                                                             ByVal message As ArraySegment(Of Byte)) As Byte()
            ' start writing all the info into a memory buffer
            Dim mem As New MemoryStream()

            ' copy the bytes encoding the length of the txPropToken            
            ' first get the bytes representing the length of the txPropToken.
            Dim txLengthBytes() As Byte

            If txPropToken IsNot Nothing Then
                txLengthBytes = BitConverter.GetBytes(txPropToken.Length)
            Else
                txLengthBytes = BitConverter.GetBytes(CInt(Fix(0)))
            End If
            mem.Write(txLengthBytes, 0, txLengthBytes.Length)

            ' copy the bytes of the transaction propagation token.
            If txPropToken IsNot Nothing Then
                mem.Write(txPropToken, 0, txPropToken.Length)
            End If

            ' copy the message bytes.
            mem.Write(message.Array, message.Offset, message.Count)

            Return mem.ToArray()
        End Function

		''' <summary>
		''' Reads out a transaction and a message from a byte buffer.        
		''' The layout of the buffer should conform with that generated by WriteTransactionMessageBuffer.
		''' This method can throw:
		'''     ArgumentNullException - if 'buffer' is null
		'''     ArgumentException     - if 'count' is less than sizeof(int)
		'''     TransactionException  - if TransactionInterop.GetTransactionFromTransmitterPropagationToken fails
		'''     InvalidDataException  - if the length of the transaction propagation token is negative or greater 
		'''                             than the length of the data in the buffer
		'''     
		''' </summary>
        Public Shared Sub ReadTransactionMessageBuffer(ByVal buffer() As Byte, ByVal count As Integer,
                                                       <System.Runtime.InteropServices.Out()> ByRef transaction As Transaction,
                                                       <System.Runtime.InteropServices.Out()> ByRef message As ArraySegment(Of Byte))

            Const sizeOfInt As Byte = 4

            If buffer Is Nothing Then
                Throw New ArgumentNullException("buffer")
            End If
            If count < sizeOfInt Then
                Throw New ArgumentException("'count' is less than size of on int.")
            End If

            Dim mem As New MemoryStream(buffer, 0, count, False, True)

            ' read the length of the transaction token.
            Dim txLengthBytes(sizeOfInt - 1) As Byte
            mem.Read(txLengthBytes, 0, sizeOfInt)
            Dim txLength = BitConverter.ToInt32(txLengthBytes, 0)

            ' check the validity of the length of the transaction propagation token.
            If txLength >= count - sizeOfInt OrElse txLength < 0 Then
                Throw New InvalidDataException("the length of the transaction propagation token read from 'buffer' is invalid")
            End If

            transaction = Nothing
            ' read the transaction propagation token and unmarshal the transaction.                   
            If txLength > 0 Then
                Dim txToken(txLength - 1) As Byte
                mem.Read(txToken, 0, txToken.Length)

                transaction = TransactionInterop.GetTransactionFromTransmitterPropagationToken(txToken)
            End If

            ' read the message.
            Dim offset = CInt(Fix(mem.Position))
            message = New ArraySegment(Of Byte)(mem.GetBuffer(), offset, count - offset)
        End Sub
	End Class
End Namespace
