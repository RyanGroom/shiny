﻿using System.Threading.Tasks;
using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BLE Characteristics")]
public class CharacteristicTests : AbstractBleTests
{
    public CharacteristicTests(ITestOutputHelper output) : base(output) { }

    
    [Theory(DisplayName = "BLE Characteristic - Characteristic - Writes")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteTests(bool withResponse)
    {
        await this.Setup();
        var value = new byte[] { 0x01, 0x02 };

        var responseType = withResponse ? BleCharacteristicEvent.Write : BleCharacteristicEvent.WriteWithoutResponse;
        var result = await this.Peripheral!.WriteCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid, value, withResponse);
        result.Event.Should().Be(responseType);
        // result.Data.Should().Be(value);
    }


    [Fact(DisplayName = "BLE Characteristic - Find Multiple")]
    public async Task FindMultipleCharacteristicTest()
    {
        await this.Setup();

        var c1 = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        var c2 = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);

        c1.Should().NotBeNull("Read Characteristic should have been found");
        c2.Should().NotBeNull("Read Characteristic should have been found");
    }


    [Fact(DisplayName = "BLE Characteristic - Read")]
    public async Task ReadTests()
    {
        await this.Setup();
        var result = await this.Peripheral!.ReadCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        result.Event.Should().Be(BleCharacteristicEvent.Read);
    }


    [Fact(DisplayName = "BLE Characteristic - Reconnect Read")]
    public async Task ReconnectReadTests()
    {
        await this.Setup();
        var result = await this.Peripheral!.ReadCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        result.Event.Should().Be(BleCharacteristicEvent.Read);
        this.Log("Initial Read Complete - Moving to Reconnection");

        this.Peripheral!.CancelConnection();
        await Task.Delay(2000);
        await this.Connect();
        result = await this.Peripheral!.ReadCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        result.Event.Should().Be(BleCharacteristicEvent.Read);
    }


    [Fact(DisplayName = "BLE Characteristic - Notification")]
    public async Task NotifyTest()
    {
        await this.Setup();
        var task = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask();

        await this.Peripheral!.WriteCharacteristicAsync(
            BleConfiguration.ServiceUuid,
            BleConfiguration.WriteCharacteristicUuid,
            new byte[] { 0x01 },
            true,
            CancellationToken.None,
            30000
        );

        var result = await task;
        result.Event.Should().Be(BleCharacteristicEvent.Notification);
    }


    [Fact(DisplayName = "BLE Characteristic - Reconnect Notification")]
    public async Task ReconnectNotifyTest()
    {
        var count = 0;
        await this.Setup();
        using var sub = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid)
            .Subscribe(x => count++);

        // trigger first notification
        await this.Peripheral!.WriteCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid, new byte[] { 0x02 }, true);
        await Task.Delay(2000);
        count.Should().Be(1);
        this.Log("Initial Test Complete - Moving to reconnection");

        // disconnecting will not remove notification, so we should expect a resubscription
        this.Peripheral!.CancelConnection();
        await this.Connect();

        this.Log("Reconnected");
        await this.Peripheral!.WriteCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid, new byte[] { 0x03 }, true);
        await Task.Delay(2000); // give a breather for resub
        count.Should().Be(2);
    }


    [Fact(DisplayName = "BLE Characteristic - Get All Characteristics")]
    public async Task GetAllCharacteristics()
    {
        await this.Setup();
        var results = await this.Peripheral!.GetAllCharacteristicsAsync();

        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid);
        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);
        
        // TODO: BLE Host is not pumping this out
        // TODO: could detect device info service?  On all android though?
        // AssertChar(results, BleConfiguration.SecondaryServiceUuid, BleConfiguration.SecondaryCharacteristicUuid1);
        // AssertChar(results, BleConfiguration.SecondaryServiceUuid, BleConfiguration.SecondaryCharacteristicUuid2);
    }


    static void AssertChar(IReadOnlyList<BleCharacteristicInfo> results, string serviceUuid, string characteristicUuid)
        => results
            .FirstOrDefault(x =>
                x.Service.Uuid.Equals(serviceUuid, StringComparison.InvariantCultureIgnoreCase) &&
                x.Uuid.Equals(characteristicUuid, StringComparison.InvariantCultureIgnoreCase)
            )
            .Should()
            .NotBeNull($"Did not find service: {serviceUuid} / characteristic: {characteristicUuid}");
    
    // [Fact(DisplayName = "BLE Characteristic - Blob Write")]
    // public async Task BlobWrite()
    // {
    //     await this.Setup();
    //
    //     // TODO: need stream
    //     this.Peripheral!.WriteCharacteristicBlob(
    //         BleConfiguration.ServiceUuid,
    //         BleConfiguration.WriteCharacteristicUuid,
    //         
    //         null,
    //         TimeSpan.FromSeconds(5)
    //     );
    // }


    [Fact(DisplayName = "BLE Characteristic - Concurrent Writes")]
    public async Task Concurrent_Writes()
    {
        await this.Setup();
        var bytes = new byte[] { 0x01 };
        var list = new List<Task<BleCharacteristicResult>>();

        var ch = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid);
        ch.Should().NotBeNull("Write characteristic was not found");
        
        for (var i = 0; i < 10; i++)
            list.Add(this.Peripheral!.WriteCharacteristicAsync(ch, bytes, true, CancellationToken.None, 5000));

        await Task.WhenAll(list);
    }


    [Fact(DisplayName = "BLE Characteristic - Concurrent Reads")]
    public async Task Concurrent_Reads()
    {
        await this.Setup();
        
        var ch = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        ch.Should().NotBeNull("Write characteristic was not found");
        
        var list = new List<Task<BleCharacteristicResult>>();
        for (var i = 0; i < 10; i++)
            list.Add(this.Peripheral!.ReadCharacteristicAsync(ch, CancellationToken.None, 5000));

        await Task.WhenAll(list);
    }
}
