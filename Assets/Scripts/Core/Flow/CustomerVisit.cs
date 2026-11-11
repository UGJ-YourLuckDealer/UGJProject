using System;
using UnityEngine;

/// <summary>
/// Counter 손님 방문 시퀀스 시작 신호 (hermit → macro).
/// 하루 첫 손님은 "Day N" 날짜 표시(DayDisplay)가 끝난 뒤, 이후 손님은 Counter 화면 Fade In이 끝난 뒤 준비 완료가 된다.
/// 방문 시퀀스(문 소리 → 초상화 Fade → 대화) 쪽은 아래처럼 쓰면 된다.
///     CustomerVisit.WhenReady(customer => StartCoroutine(VisitSequence(customer)));
/// (이미 준비됐으면 바로, 아니면 준비되는 순간 한 번 호출된다)
/// </summary>
public static class CustomerVisit
{
    // 값은 GameSession에 저장한다.

    /// <summary>이번 Counter 화면에서 방문 시퀀스를 시작해도 되는지.</summary>
    public static bool IsReady => GameSession.VisitReady;

    /// <summary>준비된 손님 번호 (DialogueManager.customer). 준비 전에는 -1.</summary>
    public static int ReadyCustomer => GameSession.ReadyCustomer;

    /// <summary>준비되면(또는 이미 준비됐으면 바로) callback(손님 번호)을 한 번 호출한다.</summary>
    public static void WhenReady(Action<int> callback)
    {
        if (callback == null) return;
        if (IsReady) { callback(ReadyCustomer); return; }

        Action<int> once = null;
        once = customer =>
        {
            GameEvents.CustomerVisitReady -= once;
            callback(customer);
        };
        GameEvents.CustomerVisitReady += once;
    }

    /// <summary>Counter 화면에 들어올 때 DayDisplay가 호출 (준비 상태 초기화).</summary>
    internal static void Reset() => GameSession.ResetVisit();

    /// <summary>날짜 표시·Fade In이 끝났을 때 DayDisplay가 호출.</summary>
    internal static void MarkReady(int customer)
    {
        GameSession.VisitReady = true;
        GameSession.ReadyCustomer = customer;
        GameEvents.RaiseCustomerVisitReady(customer);
    }
}
