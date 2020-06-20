using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
namespace DocumentSystem
{
    class DocumentSystem
    {

        public const int blockSize = 512;
        public const int blockNum = 64 * 1024;
        public const int FCBBlockNum = 64;
        public const int rootBeginAddr = 37;
        public BitArray disk = new BitArray(blockNum * blockSize + 50, false);
        public String dataFileName = "dataFile";
        public Stack FCBLevel = new Stack();
        public ArrayList curPath = new ArrayList();
        FileStream dataFile;
        public void SetInt(int data, int begin, int len)
        {
            int i;
            for (i = 0; i < len; ++i)
            {
                int val = 1 << i;
                disk[begin + len - 1 - i] = ((data & val) == val);
            }
        }
        public int GetInt(int begin, int len)
        {
            int i, data = 0;
            for (i = 0; i < len; ++i)
            {
                if (disk[begin + len - 1 - i])
                {
                    data ^= 1 << i;
                }
            }
            return data;
        }

        public void SetChar(char ch, int begin)
        {
            int i;
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            String s = new String(ch, 1);
            int data = asciiEncoding.GetBytes(s)[0];
            for (i = 0; i < 8; ++i)
            {
                int val = 1 << i;
                disk[begin + 7 - i] = ((data & val) == val);
            }
        }
        public char GetChar(int begin)
        {
            int i, data = 0;
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            for (i = 0; i < 8; ++i)
            {
                if (disk[begin + 7 - i])
                {
                    data ^= 1 << i;
                }
            }
            byte[] byteArray = new byte[] { (byte)data };
            return asciiEncoding.GetString(byteArray)[0];
        }
        public void SetString(int begin, String s)
        {
            int i;
            for (i = 0; i < s.Length; ++i)
            {
                SetChar(s[i], 8 * i + begin);
            }
        }
        public String GetString(int begin, int len)
        {
            int i;            
            List<char> lc = new List<char>();
            for (i = 0; i < len; ++i)
            {
                char ch = GetChar(begin + i * 8);
                if (ch == '\0') break;
                lc.Add(ch);
            }
            char[] t = new char[lc.Count];
            i = 0;
            foreach(char ch in lc)
            {
                t[i] = ch;
                i++;
            }
            String s = new String(t);
            return s;
        }
        public void SetBitArray(int begin, BitArray ba)
        {
            int i;
            for (i = 0; i < ba.Length; ++i)
            {
                disk[begin + i] = ba[i];
            }
        }
        public BitArray GetBitArray(int begin, int len)
        {
            BitArray ba = new BitArray(len, false);
            int i;
            for (i = 0; i < len; ++i)
            {
                ba[i] = disk[begin + i];
            }
            return ba;
        }
        public void IintSystem()
        {
            FCBLevel.Push(rootBeginAddr);
            curPath.Add("root");
            try
            {
                dataFile = new FileStream(dataFileName, FileMode.OpenOrCreate, FileAccess.Read);
                BinaryReader dataFileReader = new BinaryReader(dataFile);
                int i, j;
                for (i = 0; i < blockSize * blockNum/8; ++i)
                {
                    byte t = dataFileReader.ReadByte();
                    for(j=0;j<8;++j)
                    {
                        byte val =(byte) (1 << j);
                        if ((t & val) == val) disk[i * 8 +7- j] = true;
                    }
                }
                dataFile.Close();

            }
            catch (Exception e)
            {
                
                FirstInit();
                return;
            }

        }
        public void FirstInit()
        {
            
            disk = new BitArray(blockNum * blockSize + 50, false);
            int blockTag = 2 + FCBBlockNum;
            int i;
            if(dataFile!=null) dataFile.Close();
            SetInt(blockTag, 0, 16);
            //初始化第一个空闲物理块
            int FCBTag = 512;
            SetInt(FCBTag, 16, 21);
            //初始化第一个空闲FCB
            SetInt(blockNum - blockTag, 256, 16);//初始化空闲块数量
            int temp = GetInt(256, 16);
            SetString(37, "root");              //初始化
            disk[133] = true;                  // 根目录
            SetInt(blockTag - 1, 134, 16);    //   FCB          
            for (i = 0; i < 255; ++i)//初始化空闲FCB链表
            {
                int nextFCB = blockSize + 128 * (i + 1);
                SetInt(nextFCB, i * 128 + blockSize, 21);
            }
            for (i = 2 + FCBBlockNum; i < blockNum; ++i)//初始化空闲块链表
            {
                int nextBlock = i + 1;
                SetInt(nextBlock, i * blockSize, 16);
            }
            SetInt(GetNextFreeBlock(), (blockTag - 1) * 512, 16);
        }
        public bool WriteFile(int rootBeginAddr, BitArray dataBitset)
        {
            int i;
            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            indexBlock.num = rootFCB.indexBlock;
            while (indexBlock.IsFull())
            {
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
                indexBlock.num = nextBlock;
            }
            int endAddr = rootFCB.endOffset + indexBlock.indexes[indexBlock.endIndex - 1] * blockSize;
            for (i = 0; i < dataBitset.Length; ++i)
            {
                disk[endAddr] = dataBitset[i];
                if (endAddr % blockSize == (blockSize - 1)) //当前块被写满
                {
                    if (indexBlock.endIndex == 31)//索引块被写满
                    {
                        int nextBlock = GetNextFreeBlock();
                        if (nextBlock == 0) return false;//磁盘已满，写入失败
                        SetInt(nextBlock, indexBlock.num * blockSize + 16 * 31, 16);
                        int j;
                        for (j = 0; j < blockSize; ++j)
                        {
                            blockArray[j] = disk[j + indexBlock.num * blockSize];
                        }
                        int temp = nextBlock;
                        nextBlock = GetNextFreeBlock();
                        SetInt(nextBlock, temp * blockSize, 16);
                   
                        indexBlock.ChangeBlock(blockArray);
                        indexBlock.num = temp;
                        endAddr = nextBlock * blockSize;
                        indexBlock.endIndex = 1;
                    }
                    else
                    {
                        int nextBlock = GetNextFreeBlock();
                        if (nextBlock == 0) return false;//磁盘已满，写入失败
                        SetInt(nextBlock, indexBlock.num * blockSize + indexBlock.endIndex * 16, 16);
                        indexBlock.endIndex++;
                        endAddr = nextBlock * blockSize;
                    }
                }
                else
                {
                    endAddr++;
                }
            }
            int offset = endAddr % blockSize;
            SetInt(offset, rootBeginAddr + 113, 9);
            return true;
        }

        public int GetNextFreeBlock()
        {
            int curFreeNum = GetInt(256, 16);
            int nextBlock = GetInt(0, 16);
            if (nextBlock == 0) return 0;
            int newHead = GetInt(nextBlock * blockSize, 16);
            SetInt(0, nextBlock * blockSize, 16);
            SetInt(newHead, 0, 16);
            SetInt(curFreeNum - 1, 256, 16);
            return nextBlock;
        }

        public int GetNextFreeFCB()
        {
            int NextFCB = GetInt(16, 21);
            if (NextFCB == 0) return 0;
            int NewHead = GetInt(NextFCB, 21);
            SetInt(0, NextFCB, 21);
            SetInt(NewHead, 16, 21);
            return NextFCB;
        }
        public int CountFreeBlock()
        {
            int curFree = GetInt(256, 16);
            return curFree;
        }
        public bool WriteFile(int rootBeginAddr, String s)
        {
            int neededBlock = 0;
            neededBlock +=(int) Math.Ceiling(((decimal)s.Length * 8) / ((decimal)512));
            neededBlock += (int)Math.Ceiling(((decimal)neededBlock) / ((decimal)31));
            int freeBlockNum = CountFreeBlock();
            if (freeBlockNum < neededBlock) return false;          
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            byte[] data = asciiEncoding.GetBytes(s);
            BitArray dataArray = new BitArray(data.Length * 8);
            int i, j;
            for (i = 0; i < data.Length; ++i)
            {
                int d = (int)data[i];
                for (j = 0; j < 8; ++j)
                {
                    int val = 1 << j;
                    dataArray[i * 8 + 7 - j] = ((d & val) == val);
                }
            }
            return WriteFile(rootBeginAddr, dataArray);
        }
        public bool CreateFile(int rootBeginAddr, String fileName, bool isFolder)
        {
            if (GetInt(16, 21) == 0) return false;
            if (CountFreeBlock() < 2) return false;
            int nextFCB = GetNextFreeFCB();
            if (nextFCB == 0) return false;//无空闲FCB，创建失败
            int nextBlock = GetNextFreeBlock();
            if (nextBlock == 0) return false;//无空闲块，创建失败

            SetString(nextFCB, fileName);
            disk[nextFCB + 96] = isFolder;
            SetInt(nextBlock, nextFCB + 97, 16);

            int temp = nextBlock;
            nextBlock = GetNextFreeBlock();
            if (nextBlock == 0) return false;//无空闲块，创建失败
            SetInt(nextBlock, temp * blockSize, 16);
            BitArray childBeginAddr = new BitArray(32, false);
            BitArray chFCB = GetBitArray(nextFCB, 128);
            FCB t = new FCB(chFCB, nextFCB);
            int i;
            for (i = 0; i < 21; ++i)
            {
                int val = 1 << i;
                childBeginAddr[20 - i] = ((nextFCB & val) == val);
            }
            return WriteFile(rootBeginAddr, childBeginAddr);
        }
        public void DeleteFile(String s)
        {
            if (!IsChildExisted(s)) return;
            int childAddr = FindChild(s);
            int lastChild = FindLastChild();
            int i;
            DeleteFile(GetInt(childAddr, 21));
            for (i = 0; i < 32; ++i)
            {
                disk[childAddr + i] = disk[lastChild + i];
                disk[lastChild + i] = false;
            }
            int rootAddr = (int)FCBLevel.Peek();
            int oldOffset = GetInt(rootAddr + 113, 9);
            SetInt(oldOffset - 32, rootAddr + 113, 9);
        }
        public int FindLastChild()
        {
            int rootBeginAddr = (int)FCBLevel.Peek();

            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            indexBlock.num = rootFCB.indexBlock;
            int i;
            while (indexBlock.IsFull())
            {
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
                indexBlock.num = nextBlock;
            }
            int childAddr = indexBlock.indexes[indexBlock.endIndex - 1] * blockSize + (rootFCB.endOffset - 32);
            return childAddr;
        }
        public int FindChild(String s)
        {
            int rootBeginAddr = (int)FCBLevel.Peek();

            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            int i, j;
            while (indexBlock.IsFull())
            {
                for (i = 0; i < 31; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 16; ++j)
                    {
                        int childAddr = GetInt(curBlock * blockSize + 32 * j, 21);
                        if (GetString(childAddr, 12) == s)
                        {
                            return curBlock * blockSize + 32 * j;
                        }
                    }
                }
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
            }
            for (i = 0; i < indexBlock.endIndex - 1; ++i)
            {
                int curBlock = indexBlock.indexes[i];
                for (j = 0; j < 16; ++j)
                {
                    int childAddr = GetInt(curBlock * blockSize + 32 * j, 21);
                    if (GetString(childAddr, 12) == s)
                    {
                        return curBlock * blockSize + 32 * j;
                    }
                }
            }
            for (j = 0; j * 32 < rootFCB.endOffset; ++j)
            {
                int childAddr = GetInt(indexBlock.indexes[i] * blockSize + 32 * j, 21);
                if (GetString(childAddr, 12) == s)
                {
                    return indexBlock.indexes[i] * blockSize + 32 * j;
                }
            }
            return 0;
        }
        public void DeleteFile(int rootBeginAddr)
        {
            bool isFolder = disk[rootBeginAddr + 96];
            int i;
            if (isFolder)
            {
                BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
                FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
                BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
                IndexBlock indexBlock = new IndexBlock(blockArray);
                indexBlock.num = rootFCB.indexBlock;
                int j;
                while (indexBlock.IsFull())
                {

                    for (i = 0; i < 31; ++i)
                    {
                        int curBlock = indexBlock.indexes[i];
                        for (j = 0; j < 16; ++j)
                        {
                            DeleteFile(GetInt(curBlock * blockSize + 32 * j, 21));
                        }

                    }
                    int nextBlock = indexBlock.indexes[31];
                    for (i = 0; i < 512; ++i)
                    {
                        disk[indexBlock.num * blockSize + i] = false;//将数据擦除
                    }
                    SetInt(GetInt(0, 16), indexBlock.num * blockSize, 16);
                    SetInt(indexBlock.num, 0, 16);
                    SetInt(GetInt(256, 16) + 1, 256, 16);
                    for (i = 0; i < blockSize; ++i)
                    {
                        blockArray[i] = disk[i + nextBlock * blockSize];
                    }
                    indexBlock.ChangeBlock(blockArray);
                    indexBlock.num = nextBlock;
                }
                for (i = 0; i < indexBlock.endIndex - 1; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 16; ++j)
                    {
                        DeleteFile(GetInt(curBlock * blockSize + 32 * j, 21));
                    }
                }
                for (j = 0; j * 32 < rootFCB.endOffset; ++j)
                {
                    DeleteFile(GetInt(indexBlock.indexes[i] * blockSize + 32 * j, 21));
                }
                for (i = 0; i < 512; ++i)
                {
                    disk[indexBlock.num * blockSize + i] = false;//将数据擦除
                }
                SetInt(GetInt(0, 16), indexBlock.num * blockSize, 16);
                SetInt(indexBlock.num, 0, 16);
                SetInt(GetInt(256, 16) + 1, 256, 16);
            }
            else
            {
                BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
                FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
                BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
                IndexBlock indexBlock = new IndexBlock(blockArray);
                indexBlock.num = rootFCB.indexBlock;
                int j;
                while (indexBlock.IsFull())
                {
                    for (i = 0; i < 31; ++i)
                    {
                        int curBlock = indexBlock.indexes[i];
                        for (j = 0; j < 512; ++j)
                        {
                            disk[curBlock * blockSize + j] = false;//将数据擦除
                        }
                        SetInt(GetInt(0, 16), curBlock * blockSize, 16);
                        SetInt(curBlock, 0, 16);
                        SetInt(GetInt(256, 16) + 1, 256, 16);

                    }
                    int nextBlock = indexBlock.indexes[31];
                    for (i = 0; i < 512; ++i)
                    {
                        disk[indexBlock.num * blockSize + i] = false;//将数据擦除
                    }
                    SetInt(GetInt(0, 16), indexBlock.num * blockSize, 16);
                    SetInt(indexBlock.num, 0, 16);
                    SetInt(GetInt(256, 16) + 1, 256, 16);
                    for (i = 0; i < blockSize; ++i)
                    {
                        blockArray[i] = disk[i + nextBlock * blockSize];
                    }
                    indexBlock.ChangeBlock(blockArray);
                    indexBlock.num = nextBlock;
                }
                for (i = 0; i < indexBlock.endIndex; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 512; ++j)
                    {
                        disk[curBlock * blockSize + j] = false;//将数据擦除
                    }
                    SetInt(GetInt(0, 16), curBlock * blockSize, 16);
                    SetInt(curBlock, 0, 16);
                    SetInt(GetInt(256, 16) + 1, 256, 16);
                }
                for (j = 0; j < 512; ++j)
                {
                    disk[indexBlock.num * blockSize + j] = false;//将数据擦除
                }
                SetInt(GetInt(0, 16), indexBlock.num * blockSize, 16);
                SetInt(indexBlock.num, 0, 16);
                SetInt(GetInt(256, 16) + 1, 256, 16);
            }
            for (i = 0; i < 128; ++i)
            {
                disk[rootBeginAddr + i] = false;//擦除PCB
            }
            SetInt(GetInt(16, 21), rootBeginAddr, 21);
            SetInt(rootBeginAddr, 16, 21);
        }
        public bool CreatFileOnCur(String s, bool isFolder)
        {
            if (IsChildExisted(s)) return false;
            return CreateFile((int)FCBLevel.Peek(), s, isFolder);
        }
        public ArrayList GetAllChild(int rootBeginAddr)
        {
            ArrayList allChild = new ArrayList();

            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            int i, j;
            while (indexBlock.IsFull())
            {
                for (i = 0; i < 31; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 16; ++j)
                    {
                        allChild.Add(GetString(GetInt(curBlock * blockSize + 32 * j, 21), 12));
                    }
                }
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
            }
            for (i = 0; i < indexBlock.endIndex - 1; ++i)
            {
                int curBlock = indexBlock.indexes[i];
                for (j = 0; j < 16; ++j)
                {
                    allChild.Add(GetString(GetInt(curBlock * blockSize + 32 * j, 21), 12));
                }
            }
            for (j = 0; j * 32 < rootFCB.endOffset; ++j)
            {
                allChild.Add(GetString(GetInt(indexBlock.indexes[i] * blockSize + 32 * j, 21), 12));
            }
            return allChild;
        }

        public ArrayList GetAllChildType(int rootBeginAddr)
        {
            ArrayList allChild = new ArrayList();

            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            int i, j;
            while (indexBlock.IsFull())
            {
                for (i = 0; i < 31; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 16; ++j)
                    {
                        allChild.Add(disk[GetInt(curBlock * blockSize + 32 * j, 21)+96]);
                    }
                }
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
            }
            for (i = 0; i < indexBlock.endIndex - 1; ++i)
            {
                int curBlock = indexBlock.indexes[i];
                for (j = 0; j < 16; ++j)
                {
                    allChild.Add(disk[GetInt(curBlock * blockSize + 32 * j, 21) + 96]);
                }
            }
            for (j = 0; j * 32 < rootFCB.endOffset; ++j)
            {
                allChild.Add(disk[GetInt(indexBlock.indexes[i] * blockSize + 32 * j, 21)+96]);
            }
            return allChild;
        }
        public String ReadFile(int rootBeginAddr)
        {
            List<String> ls = new List<String>();
            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            int i, j;
            while (indexBlock.IsFull())
            {
                for (i = 0; i < 31; ++i)
                {
                  int curBlock = indexBlock.indexes[i];
                  ls.Add(GetString(curBlock * 512, 64));
                }
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);

            }
            for (i = 0; i < indexBlock.endIndex - 1; ++i)
            {
                int curBlock = indexBlock.indexes[i];
                ls.Add(GetString(curBlock * 512, 64));
            }

            String s = GetString(indexBlock.indexes[i] * blockSize, rootFCB.endOffset);
            int totLen = ls.Count * 64+s.Length;
            char[] t = new char[totLen];
            j = 0;
            i = 0;
            foreach(String str in ls)
            {
                for(j=0;j<64;++j)
                {
                    t[i] = str[j];
                    i++;
                }
            }
            for(j=0;j<s.Length;++j)
            {
                t[i] = s[j];
                i++;
            }
            return new String(t);
           
        }
        public bool OpenFile(String s)
        {
            int rootBeginAddr = (int)FCBLevel.Peek();
            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            int i, j;
            while (indexBlock.IsFull())
            {
                for (i = 0; i < 31; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 16; ++j)
                    {
                        int childAddr = GetInt(curBlock * blockSize + 32 * j, 21);
                        if (GetString(childAddr, 12) == s)
                        {
                            FCBLevel.Push(childAddr);
                            curPath.Add(s);
                            return true;
                        }
                    }
                }
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
            }
            for (i = 0; i < indexBlock.endIndex - 1; ++i)
            {
                int curBlock = indexBlock.indexes[i];
                for (j = 0; j < 16; ++j)
                {
                    int childAddr = GetInt(curBlock * blockSize + 32 * j, 21);
                    if (GetString(childAddr, 12) == s)
                    {
                        FCBLevel.Push(childAddr);
                        curPath.Add(s);
                        return true;
                    }
                }
            }
            for (j = 0; j * 32 < rootFCB.endOffset; ++j)
            {
                int childAddr = GetInt(indexBlock.indexes[i] * blockSize + 32 * j, 21);
                if (GetString(childAddr, 12) == s)
                {
                    FCBLevel.Push(childAddr);
                    curPath.Add(s);
                    return true;
                }
            }
            return false;
        }
        public void BackOff()
        {
            if (FCBLevel.Count == 1) return;
            FCBLevel.Pop();
            curPath.RemoveAt(curPath.Count - 1);
        }
        public bool GetCurType()
        {
            return disk[(int)FCBLevel.Peek() + 96];
        }
        public bool IsChildExisted(String s)
        {
            int rootBeginAddr = (int)FCBLevel.Peek();

            BitArray rootFCBArray = GetBitArray(rootBeginAddr, 128);
            FCB rootFCB = new FCB(rootFCBArray, rootBeginAddr);
            BitArray blockArray = GetBitArray(rootFCB.indexBlock * blockSize, blockSize);
            IndexBlock indexBlock = new IndexBlock(blockArray);
            int i, j;
            while (indexBlock.IsFull())
            {
                for (i = 0; i < 31; ++i)
                {
                    int curBlock = indexBlock.indexes[i];
                    for (j = 0; j < 16; ++j)
                    {
                        int childAddr = GetInt(curBlock * blockSize + 32 * j, 21);
                        if (GetString(childAddr, 12) == s)
                        {
                            return true;
                        }
                    }
                }
                int nextBlock = indexBlock.indexes[31];
                for (i = 0; i < blockSize; ++i)
                {
                    blockArray[i] = disk[i + nextBlock * blockSize];
                }
                indexBlock.ChangeBlock(blockArray);
            }
            for (i = 0; i < indexBlock.endIndex - 1; ++i)
            {
                int curBlock = indexBlock.indexes[i];
                for (j = 0; j < 16; ++j)
                {
                    int childAddr = GetInt(curBlock * blockSize + 32 * j, 21);
                    if (GetString(childAddr, 12) == s)
                    {
                        return true;
                    }
                }
            }
            for (j = 0; j * 32 < rootFCB.endOffset; ++j)
            {
                int childAddr = GetInt(indexBlock.indexes[i] * blockSize + 32 * j, 21);
                if (GetString(childAddr, 12) == s)
                {
                    return true;
                }
            }
            return false;
        }
    }
    class FCB
    {
        public int beginAddr = 0;
        public int indexBlock = 0;
        public String fileName;
        public bool fileType;
        public int endOffset = 0;
        public FCB(BitArray FCBArray, int begin)
        {

            if (FCBArray.Length != 128) return;
            beginAddr = begin;
            int i, j;
            for (i = 0; i < 12; ++i)
            {
                int temp = 0;
                for (j = 0; j < 8; ++j)
                {
                    if (FCBArray[i * 8 + 7 - j])
                    {
                        temp ^= 1 << j;
                    }
                }
                if (temp == 0) break;
                else
                {
                    System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                    byte[] byteArray = new byte[] { (byte)temp };
                    fileName += asciiEncoding.GetString(byteArray)[0];
                }
            }
            fileType = FCBArray[96];
            for (i = 0; i < 16; ++i)
            {
                if (FCBArray[112 - i])
                {
                    indexBlock ^= (1 << i);
                }
            }
            for (i = 0; i < 9; ++i)
            {
                if (FCBArray[121 - i])
                {
                    endOffset ^= (1 << i);
                }
            }
        }
    }
    class IndexBlock
    {
        public int num = 0;
        public int endIndex = 0;
        public int[] indexes = new int[32];
        public IndexBlock(BitArray blockArray)
        {
            int i;
            for (i = 0; i < 32; ++i)
            {
                int j;
                for (j = 0; j < 16; ++j)
                {
                    if (blockArray[(i + 1) * 16 - 1 - j])
                    {
                        indexes[i] ^= (1 << j);
                    }
                }
            }
            for (i = 0; i < 32; ++i)
            {
                if (indexes[31 - i] != 0) break;
            }
            endIndex = 31 - i + 1;
        }

        public void ChangeBlock(BitArray blockArray)
        {
            int i;
            for (i = 0; i < 32; ++i)
            {
                indexes[i] = 0;
                int j;
                for (j = 0; j < 16; ++j)
                {
                    if (blockArray[(i + 1) * 16 - 1 - j])
                    {
                        indexes[i] ^= (1 << j);
                    }
                }
            }
            for (i = 0; i < 32; ++i)
            {
                if (indexes[31 - i] != 0) break;
            }
            endIndex = 31 - i + 1;
        }
        public bool IsFull()
        {
            return indexes[31] != 0;
        }
    }
}
