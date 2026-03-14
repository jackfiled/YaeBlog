---
title: High Performance Computing 25 SP Non Stored Program Computing
date: 2025-08-31T13:51:17.5260660+08:00
tags:
- 高性能计算
- 学习资料
---



No Von Neumann Machines.

<!--more-->

## Application Specified Integrated Circuits

As known as **ASIC**, these hardwares can work along and are not von Neumann machines.

No stored program concept:

- Input data come in
- Pass through all circuit gates quickly
- Generate output results immediately

Advantages: performance is better.

Disadvantages: reusability is worse.

> The CPU and GPU are special kinds of ASIC.

Why we need ASIC in computing:

- Alternatives to the Moore'a law.
- High capacity and high speed.

![image-20250605185212740](./hpc-2025-non-stored-program-computing/image-20250605185212740.webp)

### Full Custom ASICs

All mask layers are customized in a full-custom ASICs.

The full-custom ASICs always can offer the highest performance and lowest part cost (smallest die size) for a given design.

A typical example of full-custom ASICs is the CPU.

The advantages and disadvantages of full-custom ASICs is shown below.

| Advantages                                                   | Disadvantages                                            |
| ------------------------------------------------------------ | -------------------------------------------------------- |
| Reducing the area                                            | The design process takes a longer time                   |
| Enhancing the performance                                    | Having more complexity in computer-aided design tool     |
| Better ability of integrating with other analog components and other pre-designed components | Requiring higher investment and skilled human resources. |

### Semi Custom ASICs

All the logical cell are predesigned and some or all of the mask layer is customized.

There are two types of semi-custom ASICs:

- Standard cell based ASICs
- Gate-array based ASICs.

The Standard cell based ASICs is also called as **Cell-based ASIC(CBIC)**.

![image-20250815093113115](./hpc-2025-non-stored-program-computing/image-20250815093113115.webp)

> The *gate* is used a unit to measure the ability of semiconductor to store logical elements.

 The semi-custom ASICs is developed as:

- Programmable Logic Array(PLA)
- Complex Programmable Logical Device(CPLD)
- Programmable Array Logical
- Field Programing Gate Array(FPGA)

#### Programmable Logical Device

An integrated circuit that can be programmed/reprogrammed with a digital logical of a curtain level.

The basic idea of PLD is an array of **AND** gates and an array of **OR** gates. Each input feeds both a non-inverting buffer and an inverting buffer  to produce the true and inverted forms of each variable. The AND outputs are called the product lines. Each product line is connected to one of the inputs of each OR gate.

Depending on the structure, the standard PLD can be divided into:

- Read Only Memory(ROM): A fixed array of AND gates and a programmable array of OR gates.
- Programmable Array Logic(PAL): A programmable array of AND gates feeding a fixed array of OR gates.
- Programmable Logic Array(PLA): A programmable array of AND gates feeding a programmable of OR gates.
- Complex Programmable Logic Device(CPLD) and Field Programmable Gate Array(FPGA): complex enough to be called as *architecture*.

![image-20250817183832472](./hpc-2025-non-stored-program-computing/image-20250817183832472.webp)



## Field Programming Gate Array

> General speaking, all semiconductor can be considered as a special kind of ASIC. But in practice, we always refer the circuit with a special function as ASIC, a circuit that can change the function as FPGA.

![image-20250612184120333](./hpc-2025-non-stored-program-computing/image-20250612184120333.webp)

### FPGA Architecture

![image-20250817184419856](./hpc-2025-non-stored-program-computing/image-20250817184419856.webp)

#### Configurable Logic Block(CLB) Architecture

The CLB consists of:

- Look-up Table(LUT):  implements the entries of a logic functions truth table.

  And some FPGAs can use the LUTs to implement small random access memory(RAM).

- Carry and Control Logic: Implements fast arithmetic operation(adders/subtractors).

- Memory Elements: configures flip flops/latches (programmable clock edges, set, reset and clock enable). These memory elements usually can be configured as shift-registers.

##### Configuring LUTs

LUT is a ram with data width of 1 bit and the content is programmed at power up. Internal signals connect to control signals of MUXs to select a values of the truth tables for any given input signals.

The below figure shows LUT working:

![image-20250817185111521](./hpc-2025-non-stored-program-computing/image-20250817185111521.webp)

The configuration memory holds the output of truth table entries, so that when the FPGA is restarting it will run with the same *program*.

And as the truth table entries are just bits, the program of FPGA is called as **BITSTREAM**, we download a bitstream to an FPGA and all LUTs will be configured using the BITSTREAM to implement the boolean logic.

##### LUT Based Ram

Let the input signal as address, the LUT will be configured as a RAM. Normally, LUT mode performs read operations, the address decoders can generate clock signal to latches for writing operation.

![image-20250817185859510](./hpc-2025-non-stored-program-computing/image-20250817185859510.webp)

#### Routing Architecture

The logic blocks are connected to each though programmable routing network. And the routing network provides routing connections among logic blocks and I/O blocks to complete a user-designed circuit.

Horizontal and vertical mesh or wire segments interconnection by programmable switches called programmable interconnect points(PIPs).

![image-20250817192006784](./hpc-2025-non-stored-program-computing/image-20250817192006784.webp)

These PIPs are implemented using a transmission gate controlled by a memory bits from the configuration memory.

Several types of PIPs are used in the FPGA:

- Cross-point: connects vertical or horizontal wire segments allowing turns.
- Breakpoint: connects or isolates 2 wire segments.
- Decoded MUX: groups of cross-points connected to a single output configured by n configuration bits.
- Non-decoded MUX: n wire segments each with a configuration bit.
- Compound cross-point: 6 breakpoint PIPs and can isolate two isolated signal nets.

![image-20250817194355228](./hpc-2025-non-stored-program-computing/image-20250817194355228.webp)

#### Input/Output Architecture

The I/O pad and surrounding supporting logical and circuitry are referred as input/input cell.

The programmable Input/Output cells consists of three parts:

- Bi-directional buffers
- Routing resources.
- Programmable I/O voltage and current levels.

![image-20250817195139631](./hpc-2025-non-stored-program-computing/image-20250817195139631.webp)

#### Fine-grained and Coarse-grained Architecture

The fine-grained architecture:

- Each logic block can implement a very simple function.
- Very efficient in implementing systolic algorithms.
- Has a large number of interconnects per logic block than the functionality they offer.

The coarse-grained architecture:

- Each logic block is relatively packed with more logic.
- Has their logic blocks packed with more functionality.
- Has fewer interconnections which leading to reduce the propagating delays encountered.

#### Interconnect Devices

FPGAs are based on an array of logic modules and uncommitted wires to route signal.

Three types of interconnected devices have been commonly used to connect there wires:

- Static random access memory (SRAM) based
- Anti-fuse based
- EEPROM based

### FPGA Design Flow

![image-20250817195714935](./hpc-2025-non-stored-program-computing/image-20250817195714935.webp)

![image-20250817200350750](./hpc-2025-non-stored-program-computing/image-20250817200350750.webp)

The FPGA configuration techniques contains:

- Full configuration and read back.
- Partial re-configuration and read back.
- Compressed configuration.

Based on the partially reconfiguration, the runtime reconfiguration is development. The area to be reconfigured is changed based on run-time.

#### Hardware Description Languages(HDL)

There are three languages targeting FPGAs:

- VHDL: VHSIC Hardware Description Language.
- Verilog
- OpenCL

The first two language are typical HDL:

| Verilog                                | VHDL                            |
| -------------------------------------- | ------------------------------- |
| Has fixed data types.                  | Has abstract data types.        |
| Relatively easy to learn.              | Relatively difficult to learn.  |
| Good gate level timing.                | Poor level gate timing.         |
| Interpreted constructs.                | Compiled constructs.            |
| Limited design reusability.            | Good design reusability.        |
| Doesn't support structure replication. | Supports structure replication. |
| Limited design management.             | Good design management.         |

The OpenCL is not an traditional hardare description language. And OpenCL needs to turn the thread parallelism into hardware parallelism, called **pipeline parallelism**.

The follow figure shows how the OpenCL-FPGA compiler turns an vector adding function into the circuit.

![image-20250829210329225](./hpc-2025-non-stored-program-computing/image-20250829210329225.webp)

The compiler generates three stages for this function:

1. In the first stage, two loading units are used.
2. In the second stage, one adding unit is used.
3. In the third stage, one storing unit is used.

Once cycle,  the thread `N` is clocked in the first stage, loading values from the array meanwhile, the thread `N  - 1` is in the second stage, adding values from the array and the thread `N - 2` is in the third stage, storing value into the target array.

So different from the CPU and GPU, the OpenCL on the FPGA has two levels of parallelism:

- Pipelining
- Replication of the kernels and having them run concurrently.

