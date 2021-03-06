﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbox360;
using Xbox360.XEX;

namespace Xenious.Xecutable
{
    public class LocalAppImport
    {
        public string filename;
        public bool include = false;
    }
    public class KernalImport
    {
        public string filename;
        public bool include = false;
        
        public bool is_binary()
        {
            return filename.Substring(-4, 4) == ".xex";
        }
    }
    public class XEXLoader
    {
        public static bool xex_is_encrypted_or_compressed(string file)
        {
            try
            {
                XenonExecutable xex = new XenonExecutable(file);
                xex.read_header();
                int x = xex.parse_optional_headers();

                if(x == 0)
                {
                    if (xex.base_file_info_h.enc_type == XeEncryptionType.Encrypted ||
                        xex.base_file_info_h.comp_type == XeCompressionType.Compressed ||
                        xex.base_file_info_h.comp_type == XeCompressionType.DeltaCompressed)
                    {
                        return true;
                    }
                }
            }
            catch { throw new Exception("Unable to parse executable..."); }
            return false;
        }
        public static bool import_libary_exists(string import_name)
        {
            foreach(string import in Xenious.Program.imports_available)
            {
                if(import_name == Path.GetFileName(import))
                {
                    return true;
                }
            }
            return false;
        }
        public static Xbox360.XenonExecutable get_import_libary(string import_name)
        {
            if(import_libary_exists(import_name))
            {
                return new Xbox360.XenonExecutable(AppDomain.CurrentDomain.BaseDirectory + "/kernal/imports/" + import_name);
            }
            throw new Exception("Unable to import libary from local import directory, Import Required : " + import_name);
        }

        public static List<Xenious.Database.PEFunction> get_function_funcs(List<byte[]> out_ops)
        {
            List<Xenious.Database.PEFunction> result = new List<Xenious.Database.PEFunction>();



            return result;
        }
        public static void load_imports_from_rdata(Xenious.Database.PEFileDatabase pe_db, Xbox360.Kernal.Memory.XboxMemory memory, int import_id = -1)
        {
            #region Get section index of rdata.
            int rdata_idx = 0;
            foreach (Xenious.Database.PEFileSection sec in pe_db.sections)
            {
                if (sec.section_name == ".rdata")
                {
                    break;
                }
                else
                {
                    rdata_idx++;
                }
            }
            #endregion
            memory.Position = pe_db.sections[rdata_idx].start_address;

            pe_db.sections[rdata_idx].imports = new List<Xenious.Database.PEImport>();
            for (int i = 0; i < memory.MainApp.import_libs.Count; i++)
            {
                UInt16 id = 1;
                UInt16 ord = 1;
                while (true)
                {
                    id = BitConverter.ToUInt16(memory.ReadBytes(2, BitConverter.IsLittleEndian), 0);
                    ord = BitConverter.ToUInt16(memory.ReadBytes(2, BitConverter.IsLittleEndian), 0);

                    if (ord != 0)
                    {
                        pe_db.sections[rdata_idx].imports.Add(new Xenious.Database.PEImport()
                        {
                            kernel_id = id,
                            ordinal = ord
                        });
                    }
                    else
                    {
                        break;
                    }
                    
                }
            }
            return;
        }
        public static bool load_mainapp_from_load_address(Xenious.Database.PEFileDatabase pe_db, Xbox360.Kernal.Memory.XboxMemory memory)
        {
            #region Get section index of starting entry.
            int section_idx = 0;
            foreach(Xenious.Database.PEFileSection sec in pe_db.sections)
            {
                if(memory.MainApp.exe_entry_point <= sec.end_address && 
                   memory.MainApp.exe_entry_point >= sec.start_address)
                {
                    break;
                }
                else
                {
                    section_idx++;
                }
            }
            #endregion

            // First set entry point.
            memory.Position = memory.MainApp.exe_entry_point;

            // Now loop through until we hit a zero's to end the function of start.
            byte[] op = new byte[4] { 0x01, 0x00, 0x00, 0x00 };

            // Init Text Functions List.
            pe_db.sections[section_idx].functions = new List<Xenious.Database.PEFunction>();

            // Make First One - Start.
            Xenious.Database.PEFunction pef = new Xenious.Database.PEFunction();
            pef.func_name = "start";
            pef.start_address = memory.MainApp.exe_entry_point;
            UInt32 end_addr = memory.MainApp.exe_entry_point;
            pef.op_codes = new List<byte[]>();

            // The first function ends with 0.
            #region Load Inital Function
            while (true)
            {
                end_addr += 4;
                op = memory.ReadBytes(4, BitConverter.IsLittleEndian);
                if (BitConverter.ToUInt32(op, 0) != 0)
                {
                    pef.op_codes.Add(op);
                }
                else
                {
                    // end of function.
                    break;
                }
            }
            #endregion
            pef.end_address = end_addr;

            // Add first function to list.
            pe_db.sections[section_idx].functions.Add(pef);

            // Next Setup imports from rdata.
            load_imports_from_rdata(pe_db, memory);

            // First add the main out ops.
            List<byte[]> out_codes = pef.get_all_out_funcs();

            // Next loop through until no more can be found.
            while(true)
            {
                foreach(byte[] code in out_codes)
                {
                    if(BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(code);
                    }
                    // Get Address of code.
                    Int32 addr = (Int32)(BitConverter.ToUInt32(code, 0) & 0x3FFFFFC);
                }
                break;
            }

            pef = null;

            return true;
        }
    }
}
