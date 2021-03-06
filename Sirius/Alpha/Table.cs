﻿using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Sirius
{
    public partial class Alpha
    {
        public enum Format
        {
            ___, Pcd, Bra, Mem, Mfc, Mbr, Opr, F_P
        }

        public enum Regs
        {
            // r0
            V0,
            // r1-r8
            T0, T1, T2, T3, T4, T5, T6, T7,
            // r9-r14
            S0, S1, S2, S3, S4, S5,
            // r15
            FP,
            // r16-r21
            A0, A1, A2, A3, A4, A5,
            // r22-r25
            T8, T9, T10, T11,
            // r26
            RA,
            // r27
            T12,
            // r28
            AT,
            // r29
            GP,
            // r30
            SP,
            // r31
            Zero,
        }

        private static Format[] formats =
        {
            /* 00-03 */ Format.Pcd, Format.___, Format.___, Format.___,
            /* 04-07 */ Format.___, Format.___, Format.___, Format.___,
            /* 08-0b */ Format.Mem, Format.Mem, Format.Mem, Format.Mem,
            /* 0c-0f */ Format.Mem, Format.Mem, Format.Mem, Format.Mem,
            /* 10-13 */ Format.Opr, Format.Opr, Format.Opr, Format.Opr,
            /* 14-17 */ Format.F_P, Format.F_P, Format.F_P, Format.F_P,
            /* 18-1b */ Format.Mfc, Format.___, Format.Mbr, Format.___,
            /* 1c-1f */ Format.Opr, Format.___, Format.___, Format.___,
            /* 20-23 */ Format.Mem, Format.Mem, Format.Mem, Format.Mem,
            /* 24-27 */ Format.Mem, Format.Mem, Format.Mem, Format.Mem,
            /* 28-2b */ Format.Mem, Format.Mem, Format.Mem, Format.Mem,
            /* 2c-2f */ Format.Mem, Format.Mem, Format.Mem, Format.Mem,
            /* 30-33 */ Format.Bra, Format.Bra, Format.Bra, Format.Bra,
            /* 34-37 */ Format.Bra, Format.Bra, Format.Bra, Format.Bra,
            /* 38-3b */ Format.Bra, Format.Bra, Format.Bra, Format.Bra,
            /* 3c-3f */ Format.Bra, Format.Bra, Format.Bra, Format.Bra,
        };

        private static string[] regname = (from i in Enumerable.Range(0, 32)
                                           select ((Regs)i).ToString().ToLower()).ToArray();
        private static Op[][] subops;
        private static ulong[] mask = (from i in Enumerable.Range(0, 256)
                                       select (ulong)Enumerable.Range(0, 8).Sum(
                                           j => (i & (1 << j)) == 0 ? 0L : 0xffL << (8 * j))).ToArray();

        static Alpha()
        {
            subops = new Op[0x40][];

            subops[0x10] = new Op[0x80];
            subops[0x11] = new Op[0x80];
            subops[0x12] = new Op[0x80];
            subops[0x13] = new Op[0x80];
            subops[0x14] = new Op[0x800];
            subops[0x15] = new Op[0x800];
            subops[0x16] = new Op[0x800];
            subops[0x17] = new Op[0x800];
            //subops[0x18] = new Ops[0x10000];
            subops[0x1a] = new Op[0x4];
            subops[0x1c] = new Op[0x80];

            for (int i = 0; i < 0x40; i++)
            {
                int h = i << 16;
                if (subops[i] != null)
                {
                    for (int j = 0; j < subops[i].Length; j++)
                    {
                        subops[i][j] = Enum.IsDefined(typeof(Op), h + j) ? (Op)(h + j) : Op.___;
                    }
                }
            }
        }

        public enum Op
        {
            ___ = -1,
            Call_pal = 0x000000,
            Opc01 = 0x010000,
            Opc02 = 0x020000,
            Opc03 = 0x030000,
            Opc04 = 0x040000,
            Opc05 = 0x050000,
            Opc06 = 0x060000,
            Opc07 = 0x070000,
            Lda = 0x080000,
            Ldah = 0x090000,
            Ldbu = 0x0a0000,
            Ldq_u = 0x0b0000,
            Ldwu = 0x0c0000,
            Stw = 0x0d0000,
            Stb = 0x0e0000,
            Stq_u = 0x0f0000,
            Addl = 0x100000,
            S4addl = 0x100002,
            Subl = 0x100009,
            S4subl = 0x10000b,
            Cmpbge = 0x10000f,
            S8addl = 0x100012,
            S8subl = 0x10001b,
            Cmpult = 0x10001d,
            Addq = 0x100020,
            S4addq = 0x100022,
            Subq = 0x100029,
            S4subq = 0x10002b,
            Cmpeq = 0x10002d,
            S8addq = 0x100032,
            S8subq = 0x10003b,
            Cmpule = 0x10003d,
            Addl__v = 0x100040,
            Subl__v = 0x100049,
            Cmplt = 0x10004d,
            Addq__v = 0x100060,
            Subq__v = 0x100069,
            Cmple = 0x10006d,
            And = 0x110000,
            Bic = 0x110008,
            Cmovlbs = 0x110014,
            Cmovlbc = 0x110016,
            Bis = 0x110020,
            Cmoveq = 0x110024,
            Cmovne = 0x110026,
            Ornot = 0x110028,
            Xor = 0x110040,
            Cmovlt = 0x110044,
            Cmovge = 0x110046,
            Eqv = 0x110048,
            Amask = 0x110061,
            Cmovle = 0x110064,
            Cmovgt = 0x110066,
            Implver = 0x11006c,
            Mskbl = 0x120002,
            Extbl = 0x120006,
            Insbl = 0x12000b,
            Mskwl = 0x120012,
            Extwl = 0x120016,
            Inswl = 0x12001b,
            Mskll = 0x120022,
            Extll = 0x120026,
            Insll = 0x12002b,
            Zap = 0x120030,
            Zapnot = 0x120031,
            Mskql = 0x120032,
            Srl = 0x120034,
            Extql = 0x120036,
            Sll = 0x120039,
            Insql = 0x12003b,
            Sra = 0x12003c,
            Mskwh = 0x120052,
            Inswh = 0x120057,
            Extwh = 0x12005a,
            Msklh = 0x120062,
            Inslh = 0x120067,
            Extlh = 0x12006a,
            Mskqh = 0x120072,
            Insqh = 0x120077,
            Extqh = 0x12007a,
            Mull = 0x130000,
            Mulq = 0x130020,
            Umulh = 0x130030,
            Mull__v = 0x130040,
            Mulq__v = 0x130060,
            Itofs = 0x140004,
            Sqrtf__c = 0x14000a,
            Sqrts__c = 0x14000b,
            Itoff = 0x140014,
            Itoft = 0x140024,
            Sqrtg__c = 0x14002a,
            Sqrtt__c = 0x14002b,
            Sqrts__m = 0x14004b,
            Sqrtt__m = 0x14006b,
            Sqrtf = 0x14008a,
            Sqrts = 0x14008b,
            Sqrtg = 0x1400aa,
            Sqrtt = 0x1400ab,
            Sqrts__d = 0x1400cb,
            Sqrtt__d = 0x1400eb,
            Sqrtf__uc = 0x14010a,
            Sqrts__uc = 0x14010b,
            Sqrtg__uc = 0x14012a,
            Sqrtt__uc = 0x14012b,
            Sqrts__um = 0x14014b,
            Sqrtt__um = 0x14016b,
            Sqrtf__u = 0x14018a,
            Sqrts__u = 0x14018b,
            Sqrtg__u = 0x1401aa,
            Sqrtt__u = 0x1401ab,
            Sqrts__ud = 0x1401cb,
            Sqrtt__ud = 0x1401eb,
            Sqrtf__sc = 0x14040a,
            Sqrtg__sc = 0x14042a,
            Sqrtf__s = 0x14048a,
            Sqrtg__s = 0x1404aa,
            Sqrtf__suc = 0x14050a,
            Sqrts__suc = 0x14050b,
            Sqrtg__suc = 0x14052a,
            Sqrtt__suc = 0x14052b,
            Sqrts__sum = 0x14054b,
            Sqrtt__sum = 0x14056b,
            Sqrtf__su = 0x14058a,
            Sqrts__su = 0x14058b,
            Sqrtg__su = 0x1405aa,
            Sqrtt__su = 0x1405ab,
            Sqrts__sud = 0x1405cb,
            Sqrtt__sud = 0x1405eb,
            Sqrts__suic = 0x14070b,
            Sqrtt__suic = 0x14072b,
            Sqrts__suim = 0x14074b,
            Sqrtt__suim = 0x14076b,
            Sqrts__sui = 0x14078b,
            Sqrtt__sui = 0x1407ab,
            Sqrts__suid = 0x1407cb,
            Sqrtt__suid = 0x1407eb,
            Addf__c = 0x150000,
            Subf__c = 0x150001,
            Mulf__c = 0x150002,
            Divf__c = 0x150003,
            Cvtdg__c = 0x15001e,
            Addg__c = 0x150020,
            Subg__c = 0x150021,
            Mulg__c = 0x150022,
            Divg__c = 0x150023,
            Cvtgf__c = 0x15002c,
            Cvtgd__c = 0x15002d,
            Cvtgq__c = 0x15002f,
            Cvtqf__c = 0x15003c,
            Cvtqg__c = 0x15003e,
            Addf = 0x150080,
            Subf = 0x150081,
            Mulf = 0x150082,
            Divf = 0x150083,
            Cvtdg = 0x15009e,
            Addg = 0x1500a0,
            Subg = 0x1500a1,
            Mulg = 0x1500a2,
            Divg = 0x1500a3,
            Cmpgeq = 0x1500a5,
            Cmpglt = 0x1500a6,
            Cmpgle = 0x1500a7,
            Cvtgf = 0x1500ac,
            Cvtgd = 0x1500ad,
            Cvtgq = 0x1500af,
            Cvtqf = 0x1500bc,
            Cvtqg = 0x1500be,
            Addf__uc = 0x150100,
            Subf__uc = 0x150101,
            Mulf__uc = 0x150102,
            Divf__uc = 0x150103,
            Cvtdg__uc = 0x15011e,
            Addg__uc = 0x150120,
            Subg__uc = 0x150121,
            Mulg__uc = 0x150122,
            Divg__uc = 0x150123,
            Cvtgf__uc = 0x15012c,
            Cvtgd__uc = 0x15012d,
            Cvtgq__vc = 0x15012f,
            Addf__u = 0x150180,
            Subf__u = 0x150181,
            Mulf__u = 0x150182,
            Divf__u = 0x150183,
            Cvtdg__u = 0x15019e,
            Addg__u = 0x1501a0,
            Subg__u = 0x1501a1,
            Mulg__u = 0x1501a2,
            Divg__u = 0x1501a3,
            Cvtgf__u = 0x1501ac,
            Cvtgd__u = 0x1501ad,
            Cvtgq__v = 0x1501af,
            Addf__sc = 0x150400,
            Subf__sc = 0x150401,
            Mulf__sc = 0x150402,
            Divf__sc = 0x150403,
            Cvtdg__sc = 0x15041e,
            Addg__sc = 0x150420,
            Subg__sc = 0x150421,
            Mulg__sc = 0x150422,
            Divg__sc = 0x150423,
            Cvtgf__sc = 0x15042c,
            Cvtgd__sc = 0x15042d,
            Cvtgq__sc = 0x15042f,
            Addf__s = 0x150480,
            Subf__s = 0x150481,
            Mulf__s = 0x150482,
            Divf__s = 0x150483,
            Cvtdg__s = 0x15049e,
            Addg__s = 0x1504a0,
            Subg__s = 0x1504a1,
            Mulg__s = 0x1504a2,
            Divg__s = 0x1504a3,
            Cmpgeq__s = 0x1504a5,
            Cmpglt__s = 0x1504a6,
            Cmpgle__s = 0x1504a7,
            Cvtgf__s = 0x1504ac,
            Cvtgd__s = 0x1504ad,
            Cvtgq__s = 0x1504af,
            Addf__suc = 0x150500,
            Subf__suc = 0x150501,
            Mulf__suc = 0x150502,
            Divf__suc = 0x150503,
            Cvtdg__suc = 0x15051e,
            Addg__suc = 0x150520,
            Subg__suc = 0x150521,
            Mulg__suc = 0x150522,
            Divg__suc = 0x150523,
            Cvtgf__suc = 0x15052c,
            Cvtgd__suc = 0x15052d,
            Cvtgq__svc = 0x15052f,
            Addf__su = 0x150580,
            Subf__su = 0x150581,
            Mulf__su = 0x150582,
            Divf__su = 0x150583,
            Cvtdg__su = 0x15059e,
            Addg__su = 0x1505a0,
            Subg__su = 0x1505a1,
            Mulg__su = 0x1505a2,
            Divg__su = 0x1505a3,
            Cvtgf__su = 0x1505ac,
            Cvtgd__su = 0x1505ad,
            Cvtgq__sv = 0x1505af,
            Adds__c = 0x160000,
            Subs__c = 0x160001,
            Muls__c = 0x160002,
            Divs__c = 0x160003,
            Addt__c = 0x160020,
            Subt__c = 0x160021,
            Mult__c = 0x160022,
            Divt__c = 0x160023,
            Cvtts__c = 0x16002c,
            Cvttq__c = 0x16002f,
            Cvtqs__c = 0x16003c,
            Cvtqt__c = 0x16003e,
            Adds__m = 0x160040,
            Subs__m = 0x160041,
            Muls__m = 0x160042,
            Divs__m = 0x160043,
            Addt__m = 0x160060,
            Subt__m = 0x160061,
            Mult__m = 0x160062,
            Divt__m = 0x160063,
            Cvtts__m = 0x16006c,
            Cvttq__m = 0x16006f,
            Cvtqs__m = 0x16007c,
            Cvtqt__m = 0x16007e,
            Adds = 0x160080,
            Subs = 0x160081,
            Muls = 0x160082,
            Divs = 0x160083,
            Addt = 0x1600a0,
            Subt = 0x1600a1,
            Mult = 0x1600a2,
            Divt = 0x1600a3,
            Cmptun = 0x1600a4,
            Cmpteq = 0x1600a5,
            Cmptlt = 0x1600a6,
            Cmptle = 0x1600a7,
            Cvtts = 0x1600ac,
            Cvttq = 0x1600af,
            Cvtqs = 0x1600bc,
            Cvtqt = 0x1600be,
            Adds__d = 0x1600c0,
            Subs__d = 0x1600c1,
            Muls__d = 0x1600c2,
            Divs__d = 0x1600c3,
            Addt__d = 0x1600e0,
            Subt__d = 0x1600e1,
            Mult__d = 0x1600e2,
            Divt__d = 0x1600e3,
            Cvtts__d = 0x1600ec,
            Cvttq__d = 0x1600ef,
            Cvtqs__d = 0x1600fc,
            Cvtqt__d = 0x1600fe,
            Adds__uc = 0x160100,
            Subs__uc = 0x160101,
            Muls__uc = 0x160102,
            Divs__uc = 0x160103,
            Addt__uc = 0x160120,
            Subt__uc = 0x160121,
            Mult__uc = 0x160122,
            Divt__uc = 0x160123,
            Cvtts__uc = 0x16012c,
            Cvttq__vc = 0x16012f,
            Adds__um = 0x160140,
            Subs__um = 0x160141,
            Muls__um = 0x160142,
            Divs__um = 0x160143,
            Addt__um = 0x160160,
            Subt__um = 0x160161,
            Mult__um = 0x160162,
            Divt__um = 0x160163,
            Cvtts__um = 0x16016c,
            Cvttq__vm = 0x16016f,
            Adds__u = 0x160180,
            Subs__u = 0x160181,
            Muls__u = 0x160182,
            Divs__u = 0x160183,
            Addt__u = 0x1601a0,
            Subt__u = 0x1601a1,
            Mult__u = 0x1601a2,
            Divt__u = 0x1601a3,
            Cvtts__u = 0x1601ac,
            Cvttq__v = 0x1601af,
            Adds__ud = 0x1601c0,
            Subs__ud = 0x1601c1,
            Muls__ud = 0x1601c2,
            Divs__ud = 0x1601c3,
            Addt__ud = 0x1601e0,
            Subt__ud = 0x1601e1,
            Mult__ud = 0x1601e2,
            Divt__ud = 0x1601e3,
            Cvtts__ud = 0x1601ec,
            Cvttq__vd = 0x1601ef,
            Cvtst = 0x1602ac,
            Adds__suc = 0x160500,
            Subs__suc = 0x160501,
            Muls__suc = 0x160502,
            Divs__suc = 0x160503,
            Addt__suc = 0x160520,
            Subt__suc = 0x160521,
            Mult__suc = 0x160522,
            Divt__suc = 0x160523,
            Cvtts__suc = 0x16052c,
            Cvttq__svc = 0x16052f,
            Adds__sum = 0x160540,
            Subs__sum = 0x160541,
            Muls__sum = 0x160542,
            Divs__sum = 0x160543,
            Addt__sum = 0x160560,
            Subt__sum = 0x160561,
            Mult__sum = 0x160562,
            Divt__sum = 0x160563,
            Cvtts__sum = 0x16056c,
            Cvttq__svm = 0x16056f,
            Adds__su = 0x160580,
            Subs__su = 0x160581,
            Muls__su = 0x160582,
            Divs__su = 0x160583,
            Addt__su = 0x1605a0,
            Subt__su = 0x1605a1,
            Mult__su = 0x1605a2,
            Divt__su = 0x1605a3,
            Cmptun__su = 0x1605a4,
            Cmpteq__su = 0x1605a5,
            Cmptlt__su = 0x1605a6,
            Cmptle__su = 0x1605a7,
            Cvtts__su = 0x1605ac,
            Cvttq__sv = 0x1605af,
            Adds__sud = 0x1605c0,
            Subs__sud = 0x1605c1,
            Muls__sud = 0x1605c2,
            Divs__sud = 0x1605c3,
            Addt__sud = 0x1605e0,
            Subt__sud = 0x1605e1,
            Mult__sud = 0x1605e2,
            Divt__sud = 0x1605e3,
            Cvtts__sud = 0x1605ec,
            Cvttq__svd = 0x1605ef,
            Cvtst__s = 0x1606ac,
            Adds__suic = 0x160700,
            Subs__suic = 0x160701,
            Muls__suic = 0x160702,
            Divs__suic = 0x160703,
            Addt__suic = 0x160720,
            Subt__suic = 0x160721,
            Mult__suic = 0x160722,
            Divt__suic = 0x160723,
            Cvtts__suic = 0x16072c,
            Cvttq__svic = 0x16072f,
            Cvtqs__suic = 0x16073c,
            Cvtqt__suic = 0x16073e,
            Adds__suim = 0x160740,
            Subs__suim = 0x160741,
            Muls__suim = 0x160742,
            Divs__suim = 0x160743,
            Addt__suim = 0x160760,
            Subt__suim = 0x160761,
            Mult__suim = 0x160762,
            Divt__suim = 0x160763,
            Cvtts__suim = 0x16076c,
            Cvttq__svim = 0x16076f,
            Cvtqs__suim = 0x16077c,
            Cvtqt__suim = 0x16077e,
            Adds__sui = 0x160780,
            Subs__sui = 0x160781,
            Muls__sui = 0x160782,
            Divs__sui = 0x160783,
            Addt__sui = 0x1607a0,
            Subt__sui = 0x1607a1,
            Mult__sui = 0x1607a2,
            Divt__sui = 0x1607a3,
            Cvtts__sui = 0x1607ac,
            Cvttq__svi = 0x1607af,
            Cvtqs__sui = 0x1607bc,
            Cvtqt__sui = 0x1607be,
            Adds__suid = 0x1607c0,
            Subs__suid = 0x1607c1,
            Muls__suid = 0x1607c2,
            Divs__suid = 0x1607c3,
            Addt__suid = 0x1607e0,
            Subt__suid = 0x1607e1,
            Mult__suid = 0x1607e2,
            Divt__suid = 0x1607e3,
            Cvtts__suid = 0x1607ec,
            Cvttq__svid = 0x1607ef,
            Cvtqs__suid = 0x1607fc,
            Cvtqt__suid = 0x1607fe,
            Cvtlq = 0x170010,
            Cpys = 0x170020,
            Cpysn = 0x170021,
            Cpyse = 0x170022,
            Mt_fpcr = 0x170024,
            Mf_fpcr = 0x170025,
            Fcmoveq = 0x17002a,
            Fcmovne = 0x17002b,
            Fcmovlt = 0x17002c,
            Fcmovge = 0x17002d,
            Fcmovle = 0x17002e,
            Fcmovgt = 0x17002f,
            Cvtql = 0x170030,
            Cvtql__v = 0x170130,
            Cvtql__sv = 0x170530,
            Trapb = 0x180000,
            Excb = 0x180400,
            Mb = 0x184000,
            Wmb = 0x184400,
            Fetch = 0x188000,
            Fetch_m = 0x18a000,
            Rpcc = 0x18c000,
            Rc = 0x18e000,
            Ecb = 0x18e800,
            Rs = 0x18f000,
            Wh64 = 0x18f800,
            Wh64en = 0x18fc00,
            Pal19 = 0x190000,
            Jmp = 0x1a0000,
            Jsr = 0x1a0001,
            Ret = 0x1a0002,
            Jsr_coroutine = 0x1a0003,
            Pal1b = 0x1b0000,
            Sextb = 0x1c0000,
            Sextw = 0x1c0001,
            Ctpop = 0x1c0030,
            Perr = 0x1c0031,
            Ctlz = 0x1c0032,
            Cttz = 0x1c0033,
            Unpkbw = 0x1c0034,
            Unpkbl = 0x1c0035,
            Pkwb = 0x1c0036,
            Pklb = 0x1c0037,
            Minsb8 = 0x1c0038,
            Minsw4 = 0x1c0039,
            Minub8 = 0x1c003a,
            Minuw4 = 0x1c003b,
            Maxub8 = 0x1c003c,
            Maxuw4 = 0x1c003d,
            Maxsb8 = 0x1c003e,
            Maxsw4 = 0x1c003f,
            Ftoit = 0x1c0070,
            Ftois = 0x1c0078,
            Pal1d = 0x1d0000,
            Pal1e = 0x1e0000,
            Pal1f = 0x1f0000,
            Ldf = 0x200000,
            Ldg = 0x210000,
            Lds = 0x220000,
            Prefetch_m = 0x220000,
            Prefetch_men = 0x230000,
            Ldt = 0x230000,
            Stf = 0x240000,
            Stg = 0x250000,
            Sts = 0x260000,
            Stt = 0x270000,
            Prefetch = 0x280000,
            Ldl = 0x280000,
            Ldq = 0x290000,
            Prefetch_en = 0x290000,
            Ldl_l = 0x2a0000,
            Ldq_l = 0x2b0000,
            Stl = 0x2c0000,
            Stq = 0x2d0000,
            Stl_c = 0x2e0000,
            Stq_c = 0x2f0000,
            Br = 0x300000,
            Fbeq = 0x310000,
            Fblt = 0x320000,
            Fble = 0x330000,
            Bsr = 0x340000,
            Fbne = 0x350000,
            Fbge = 0x360000,
            Fbgt = 0x370000,
            Blbc = 0x380000,
            Beq = 0x390000,
            Blt = 0x3a0000,
            Ble = 0x3b0000,
            Blbs = 0x3c0000,
            Bne = 0x3d0000,
            Bge = 0x3e0000,
            Bgt = 0x3f0000,
        }
    }
}
